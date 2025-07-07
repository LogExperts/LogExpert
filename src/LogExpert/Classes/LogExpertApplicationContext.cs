using LogExpert.Core.Interface;
using System;
using System.Windows.Forms;

namespace LogExpert.Classes;

internal class LogExpertApplicationContext : ApplicationContext
{
    #region Fields

    private readonly LogExpertProxy _proxy;

    #endregion

    #region cTor

    public LogExpertApplicationContext(LogExpertProxy proxy, ILogTabWindow firstLogWin)
    {
        _proxy = proxy;
        _proxy.LastWindowClosed += OnProxyLastWindowClosed;
        firstLogWin.Show();
    }

    #endregion

    #region Events handler

    private void OnProxyLastWindowClosed(object sender, EventArgs e)
    {
        ExitThread();
        Application.Exit();
    }

    #endregion
}