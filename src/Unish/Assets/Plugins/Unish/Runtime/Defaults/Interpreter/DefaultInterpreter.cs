using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using VDictionary = System.Collections.Generic.Dictionary<string, Rili.Debug.Shell.UnishVariable>;

namespace Rili.Debug.Shell
{
    public class DefaultInterpreter : IUnishInterpreter
    {
        public  IUnishEnv                                     BuiltInEnv { protected get; set; }
        public  IDictionary<string, string>                   Aliases    { get;           private set; }
        public  IReadOnlyDictionary<string, UnishCommandBase> Commands   => mRepository.Map;
        private IUnishCommandRepository                       mRepository;

        // ----------------------------------
        // public methods
        // ----------------------------------
        public DefaultInterpreter(IUnishCommandRepository cmdRepo = default)
        {
            mRepository = cmdRepo ?? DefaultCommandRepository.Instance;
        }

        public async UniTask InitializeAsync()
        {
            Aliases = new Dictionary<string, string>();
            await mRepository.InitializeAsync();
        }

        public async UniTask FinalizeAsync()
        {
            await mRepository.FinalizeAsync();
            Aliases     = null;
            mRepository = null;
        }

        public async UniTask RunCommandAsync(IUnishProcess shell, string cmd)
        {
            if (string.IsNullOrWhiteSpace(cmd))
            {
                return;
            }

            cmd = cmd.TrimStart();


            // エイリアス解決
            foreach (var kv in Aliases)
            {
                if (cmd.TrimEnd() == kv.Key)
                {
                    cmd = kv.Value;
                    break;
                }

                if (cmd.StartsWith(kv.Key + " "))
                {
                    cmd = kv.Value + cmd.Substring(kv.Key.Length);
                    break;
                }
            }

            var noVariableInput = UnishCommandUtils.ParseVariables(cmd, shell.Env);
            var tokens          = UnishCommandUtils.CommandToTokens(noVariableInput);
            var commands        = UnishCommandUtils.SplitTokens(tokens);

            var pipeInString  = "";
            var pipeOutString = "";

            foreach (var command in commands)
            {
                var cmdToken   = command.Command;
                var arguments  = command.Arguments;
                var isPipedIn  = command.IsPipeIn;
                var isPipedOut = command.IsPipeOut;

                // シェル変数への代入命令は特別扱い
                var eqIdx = cmdToken.IndexOf('=');
                if (eqIdx > 0 && eqIdx < cmdToken.Length - 1)
                {
                    var left  = cmdToken.Substring(0, eqIdx);
                    var right = cmdToken.Substring(eqIdx + 1);
                    right = UnishCommandUtils.RemoveQuotesIfExist(right);
                    if (shell.Env.BuiltIn.ContainsKey(left))
                    {
                        shell.Env.BuiltIn.Set(left, right);
                    }
                    else
                    {
                        shell.Env.Shell.Set(left, right);
                    }

                    continue;
                }

                // 対応するコマンドが存在すれば実行
                if (mRepository.Map.TryGetValue(cmdToken, out var cmdInstance)
                    || mRepository.Map.TryGetValue("@" + cmdToken, out cmdInstance))
                {
                    try
                    {
                        var parsed = await ParseArguments(shell.IO, cmdInstance, cmdToken, arguments);
                        if (parsed?.IsSucceeded ?? false)
                        {
                            // リダイレクトに応じてIO差し替え
                            pipeInString = pipeOutString;
                            UnishFdIn pipeIn = null;
                            if (isPipedIn)
                            {
                                pipeIn = _ => UniTaskAsyncEnumerable.Create<string>(async (writer, token) =>
                                {
                                    var b = 0;
                                    for (var i = 0; i < pipeInString.Length; i++)
                                    {
                                        if (pipeInString[i] == '\n')
                                        {
                                            await writer.YieldAsync(pipeInString.Substring(b, i - b));
                                            b = i + 1;
                                        }
                                    }

                                    if (b < pipeInString.Length)
                                    {
                                        await writer.YieldAsync(pipeInString.Substring(b, pipeInString.Length - b));
                                    }
                                });
                            }

                            UnishFdOut pipeOut = null;
                            if (isPipedOut)
                            {
                                pipeOut = text =>
                                {
                                    pipeOutString += text;
                                    return default;
                                };
                            }

                            var io = ConstructIO(parsed, shell.IO, shell.Directory, BuiltInEnv, pipeIn, pipeOut);

                            // ビルトインコマンド以外の場合はシェルプロセスをfork
                            var runnerShell = cmdInstance.IsBuiltIn ? shell : shell.Fork(io);

                            // 実行
                            await cmdInstance.Run(runnerShell, parsed.Params, parsed.Options);
                        }
                    }
                    catch (Exception e)
                    {
                        await shell.IO.Err(e);
                    }

                    continue;
                }

                // コマンドが見つからなかった場合の追加評価処理が定義されていれば実行
                if (!await TryRunUnknownCommand(shell, cmd))
                {
                    await shell.IO.Err(new Exception("Unknown Command. Enter 'h' to show help."));
                }

                await UniTask.Yield();
            }
        }


        // ----------------------------------
        // protected methods
        // ----------------------------------
        protected virtual UniTask<bool> TryRunUnknownCommand(IUnishProcess process, string cmd)
        {
            return UniTask.FromResult(false);
        }

        // ----------------------------------
        // private methods
        // ----------------------------------

        protected class UnishCommandParseResult
        {
            public bool        IsSucceeded;
            public VDictionary Params;
            public VDictionary Options;
            public string      RedirectIn;
            public string      RedirectOut;
            public string      RedirectErr;
            public bool        IsRedirectOutAppend;
            public bool        IsRedirectErrAppend;
        }

        private static async UniTask<UnishCommandParseResult> ParseArguments(
            UnishIOs io,
            UnishCommandBase targetCommand,
            string op,
            IEnumerable<(string Token, UnishCommandTokenType TokenType)> arguments)
        {
            var ret      = new UnishCommandParseResult();
            var dParams  = new VDictionary();
            var dOptions = new VDictionary();
            dParams["0"] = new UnishVariable("0", op);

            var parsingOptionName    = "";
            var parsingOptionSName   = "";
            var parsingOptionType    = UnishVariableType.Unit;
            var parsingOptionDefault = "";

            var currentParamIndex = 0;

            bool SetOption(string name, string sname, UnishVariableType type, string value)
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    var v = new UnishVariable(name, type, value);
                    if (v.Type == UnishVariableType.Error)
                    {
                        return false;
                    }
                    dOptions[name] = v;
                }

                if (!string.IsNullOrWhiteSpace(sname))
                {
                    var v = new UnishVariable(sname, type, value);
                    if (v.Type == UnishVariableType.Error)
                    {
                        return false;
                    }
                    dOptions[sname] = v;
                }

                return true;
            }

            foreach (var (token, tokenType) in arguments)
            {
                // 前のトークンがオプションで、引数を必要としていて、現在のトークンがパラメータ以外なら既定値を入れて生成
                if (parsingOptionType != UnishVariableType.Unit && tokenType != UnishCommandTokenType.Param)
                {
                    // 既定値がなければエラー
                    if (parsingOptionDefault == null)
                    {
                        await io.Err(new ArgumentException($"The option {parsingOptionName} requires argument"));
                        await targetCommand.WriteUsage(io, op);
                        return default;
                    }

                    if (!SetOption(parsingOptionName, parsingOptionSName, parsingOptionType, parsingOptionDefault))
                    {
                        await io.Err(new Exception($"Type mismatch: {parsingOptionDefault} is not {parsingOptionType}."));
                        await targetCommand.WriteUsage(io, op);
                        return default;
                    }
                    parsingOptionType    = UnishVariableType.Unit;
                    parsingOptionName    = "";
                    parsingOptionSName   = "";
                    parsingOptionDefault = "";
                }

                // 現在のトークンがオプションなら
                if (tokenType is UnishCommandTokenType.OptionShort or UnishCommandTokenType.OptionLong)
                {
                    // 現在のトークンに相当するオプションを検索
                    var found = false;
                    foreach (var expected in targetCommand.Options)
                    {
                        if (token != expected.name && token != expected.sName)
                        {
                            continue;
                        }

                        // 引数を必要としないならこの場で生成
                        if (expected.type == UnishVariableType.Unit)
                        {
                            if (!SetOption(expected.name, expected.sName, expected.type, token))
                            {
                                await io.Err(new Exception($"Type mismatch: {token} is not {expected.type}."));
                                await targetCommand.WriteUsage(io, op);
                                return default;
                            }
                            parsingOptionType        = UnishVariableType.Unit;
                            parsingOptionName        = "";
                            parsingOptionSName       = "";
                            parsingOptionDefault     = "";
                        }
                        // 引数を必要とするなら、次のトークンを引数として判定するための情報を確保
                        else
                        {
                            parsingOptionType    = expected.type;
                            parsingOptionName    = expected.name;
                            parsingOptionSName   = expected.sName;
                            parsingOptionDefault = expected.defVal;
                        }

                        found = true;
                        break;
                    }

                    // 見つからなければエラー
                    if (!found)
                    {
                        await io.Err(new ArgumentException($"Unknown option: {token}"));
                        await targetCommand.WriteUsage(io, op);
                        return default;
                    }
                }

                // 前のトークンがオプションかつ要引数で、現在のトークンがパラメータの場合
                if (parsingOptionType != UnishVariableType.Unit && tokenType == UnishCommandTokenType.Param)
                {
                    // 現在のトークンをオプションの引数として格納
                    if (!SetOption(parsingOptionName, parsingOptionSName, parsingOptionType, token))
                    {
                        await io.Err(new Exception($"Type mismatch: {token} is not {parsingOptionType}."));
                        await targetCommand.WriteUsage(io, op);
                        return default;
                    }

                    parsingOptionType    = UnishVariableType.Unit;
                    parsingOptionName    = "";
                    parsingOptionDefault = "";
                    continue;
                }

                // 現在のトークンがパラメータであり、期待されるパラメータの個数に収まっている場合
                if (tokenType == UnishCommandTokenType.Param && currentParamIndex < targetCommand.Params.Length)
                {
                    var expectedParam = targetCommand.Params[currentParamIndex];
                    // 現在のトークンをコマンドの引数として格納
                    var arg = new UnishVariable(expectedParam.name, expectedParam.type, token);
                    // $1, $2,...にも格納
                    dParams[$"{currentParamIndex + 1}"] = dParams[expectedParam.name] = arg;
                    // 型エラーチェック
                    if (arg.Type == UnishVariableType.Error)
                    {
                        await io.Err(new Exception($"Type mismatch: {token} is not {expectedParam.type}."));
                        await targetCommand.WriteUsage(io, op);
                        return default;
                    }

                    // 次に期待されるコマンドにindexを進める
                    currentParamIndex++;
                    continue;
                }

                // 現在のトークンはパラメータだが、期待されるパラメータ数を超過している場合
                if (tokenType == UnishCommandTokenType.Param)
                {
                    // string入力として $1, $2,...にのみ格納
                    var name = $"{currentParamIndex + 1}";
                    dParams[name] = new UnishVariable(name, token);
                    currentParamIndex++;
                    continue;
                }

                // 他のトークンタイプの場合
                switch (tokenType)
                {
                    case UnishCommandTokenType.RedirectIn:
                        ret.RedirectIn = token;
                        break;
                    case UnishCommandTokenType.RedirectOut:
                        ret.RedirectOut         = token;
                        ret.IsRedirectOutAppend = false;
                        break;
                    case UnishCommandTokenType.RedirectErr:
                        ret.RedirectErr         = token;
                        ret.IsRedirectErrAppend = false;
                        break;
                    case UnishCommandTokenType.RedirectOutAppend:
                        ret.RedirectOut         = token;
                        ret.IsRedirectOutAppend = true;
                        break;
                    case UnishCommandTokenType.RedirectErrAppend:
                        ret.RedirectErr         = token;
                        ret.IsRedirectErrAppend = true;
                        break;
                }
            }

            // 最後のオプションが引数を必要とするものだった場合、既定値を格納
            if (parsingOptionType != UnishVariableType.Unit)
            {
                if (parsingOptionDefault == null)
                {
                    await io.Err(new Exception($"Missing option: {(string.IsNullOrWhiteSpace(parsingOptionName) ? parsingOptionSName : parsingOptionName)}"));
                    await targetCommand.WriteUsage(io, op);
                    return default;
                }

                if (!SetOption(parsingOptionName, parsingOptionSName, parsingOptionType, parsingOptionDefault))
                {
                    await io.Err(new Exception($"Type mismatch: {parsingOptionDefault} is not {parsingOptionType}."));
                    await targetCommand.WriteUsage(io, op);
                    return default;
                }
            }


            var assembledParams = "";
            var listedParams    = new string[currentParamIndex];
            for (var i = 1; i <= currentParamIndex; i++)
            {
                var p = dParams[$"{i}"].S;
                assembledParams += p;
                if (i < currentParamIndex)
                {
                    assembledParams += " ";
                }

                listedParams[i - 1] = p;
            }

            var assembledOptions = "";
            foreach (var option in dOptions.Keys)
            {
                if (option.Length == 1)
                {
                    assembledOptions += option;
                }
            }

            // パラメータの個数を格納
            dParams["#"] = new UnishVariable("#", currentParamIndex);
            dParams["*"] = new UnishVariable("*", assembledParams);
            dParams["@"] = new UnishVariable("@", listedParams);
            dParams["-"] = new UnishVariable("-", assembledOptions);

            // 入力だけでは期待されるパラメータリストを全て満たせない場合、それぞれ既定値を格納
            while (currentParamIndex < targetCommand.Params.Length)
            {
                var expectedParam = targetCommand.Params[currentParamIndex];
                if (expectedParam.defVal == null)
                {
                    await io.Err(new Exception($"Missing parameter: {expectedParam.name}"));
                    await targetCommand.WriteUsage(io, op);
                    return default;
                }

                dParams[$"{currentParamIndex + 1}"] = dParams[expectedParam.name]
                    = new UnishVariable(expectedParam.name, expectedParam.type, expectedParam.defVal);

                currentParamIndex++;
            }

            // 設定されていないオプションは既定値を格納
            foreach (var opt in targetCommand.Options)
            {
                if (!dOptions.ContainsKey(opt.name))
                {
                    // Unit型オプションは定義されているかどうかを見るので既定値は存在しない
                    if (opt.type == UnishVariableType.Unit)
                    {
                        continue;
                    }

                    // 既定値がなければエラー
                    if (opt.defVal == null)
                    {
                        await io.Err(new Exception($"Missing option: {opt.name}"));
                        await targetCommand.WriteUsage(io, op);
                        return default;
                    }

                    if (!string.IsNullOrWhiteSpace(opt.name))
                    {
                        dOptions[opt.name] = new UnishVariable(opt.name, opt.type, opt.defVal);
                    }

                    if (!string.IsNullOrWhiteSpace(opt.sName))
                    {
                        dOptions[opt.sName] = new UnishVariable(opt.sName, opt.type, opt.defVal);
                    }
                }
            }

            ret.Params      = dParams;
            ret.Options     = dOptions;
            ret.IsSucceeded = true;
            return ret;
        }

        // Construct IO after redirect and pipe are applied
        private static UnishIOs ConstructIO(
            UnishCommandParseResult parsed,
            UnishIOs stdio,
            IUnishFileSystemRoot fileSystem,
            IUnishEnv builtInEnv,
            UnishFdIn pipeIn,
            UnishFdOut pipeOut)
        {
            UnishFdIn  input;
            UnishFdOut output;
            UnishFdErr error;

            if (pipeIn != null)
            {
                input = pipeIn;
            }
            else if (string.IsNullOrEmpty(parsed.RedirectIn))
            {
                input = stdio.In;
            }
            else
            {
                input = _ => fileSystem.ReadLines(parsed.RedirectIn);
            }

            if (pipeOut != null)
            {
                output = pipeOut;
            }
            else if (string.IsNullOrEmpty(parsed.RedirectOut))
            {
                output = stdio.Out;
            }
            else if (parsed.IsRedirectOutAppend)
            {
                output = text =>
                {
                    fileSystem.Append(parsed.RedirectOut, text);
                    return UniTask.CompletedTask;
                };
            }
            else
            {
                output = text =>
                {
                    fileSystem.Write(parsed.RedirectOut, text);
                    return UniTask.CompletedTask;
                };
            }

            if (string.IsNullOrEmpty(parsed.RedirectErr))
            {
                error = stdio.Err;
            }
            else if (parsed.IsRedirectOutAppend)
            {
                error = err =>
                {
                    var message = err.Message + "\n" + err.StackTrace + "\n";
                    fileSystem.Append(parsed.RedirectErr, message);
                    return UniTask.CompletedTask;
                };
            }
            else
            {
                error = err =>
                {
                    var message = err.Message + "\n" + err.StackTrace + "\n";
                    fileSystem.Write(parsed.RedirectErr, message);
                    return UniTask.CompletedTask;
                };
            }

            return new UnishIOs(input, output, error, builtInEnv);
        }
    }
}

