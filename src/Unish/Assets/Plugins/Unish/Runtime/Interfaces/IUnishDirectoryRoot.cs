﻿namespace Rili.Debug.Shell
{
    public interface IUnishFileSystemRoot : IUnishFileSystem
    {
        IUnishFileSystem CurrentHome      { get; }
        public string    CurrentDirectory { get; }
    }
}
