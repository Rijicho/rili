# Unish
A shell-like debug console on Unity runtime (for my very personal use) (WIP)

## Dependencies
- UniTask v2
- InputSystem

## Unity version
2021.3.3f1 or newer

## Quick Start
1. Install [UniTask](https://github.com/Cysharp/UniTask)
2. Install Unish via upm
  - Use `Add package from git URL`
  - The URL is: https://github.com/Rijicho/rili.git?path=src/Unish/Assets/Plugins/Unish
3. Write a script below, attach it to any GameObject and play the scene
```C#
using Rili.Debug.Shell;
using UnityEngine;

public class Test : MonoBehaviour
{
    private void Start()
    {
        new Unish().Run();
    }
}
```

## Add commands
You can define your command classes deriving from `UnishCommandBase`, and that's the only thing you must to do.

For better initializing performance, it is recommended to define your command repository class.
It can be easily done by deriving from `DefaultCommandRepository` and override its method `GetDomainAssemblies` to make it load required assemblies only.

Please see `Examples/ExampleCustomized.cs` for customization examples.

## Usage
- `h` shows the brief help.
- Input `lc -d` to show all commands in detail.
- `q` closes the window.
- Ctrl(Command)+Arrows scroll the window. 
- Window size and font size can be changed by environment variables:
  - UNISH_CHARCNT_PER_LINE
  - UNISH_LINECNT
  - UNISH_FONTSIZE
- If you want to save the settings, please put .unishrc and .uprofile (like zsh's .zshrc and .zprofile) at `Application.PersistentDataPath`.
  - The default home path can be changed by overriding Unish's directory system by script. 
