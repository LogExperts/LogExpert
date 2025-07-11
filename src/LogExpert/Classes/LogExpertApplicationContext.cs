using System.Runtime.Versioning;
using System.Windows.Forms;

using LogExpert.Core.Interface;

namespace LogExpert.Classes;

internal class LogExpertApplicationContext : ApplicationContext
{
    #region Fields

    private readonly LogExpertProxy _proxy;

    #endregion

    #region cTor

    [SupportedOSPlatform("windows")]
    public LogExpertApplicationContext (LogExpertProxy proxy, ILogTabWindow firstLogWin)
    {
        _proxy = proxy;
        _proxy.LastWindowClosed += OnProxyLastWindowClosed;
        firstLogWin.Show();
    }

    #endregion

    #region Events handler

    [SupportedOSPlatform("windows")]
    private void OnProxyLastWindowClosed (object sender, EventArgs e)
    {
        ExitThread();
        Application.Exit();
    }

    #endregion
}