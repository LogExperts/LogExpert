﻿namespace LogExpert.Core.Interface
{
    public interface ILogTabWindow
    {
        ILogExpertProxy LogExpertProxy { get; set; }
        bool IsDisposed { get; }

        void Activate();
        object Invoke(Delegate method, params object?[]? objects);
        void LoadFiles(string[] fileNames);
        void SetForeground();
        void Show();
    }
}