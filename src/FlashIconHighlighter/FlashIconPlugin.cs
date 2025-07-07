using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Windows.Forms;

using LogExpert;

[assembly: SupportedOSPlatform("windows")]
namespace FlashIconHighlighter;

internal class FlashIconPlugin : IKeywordAction
{
    #region Properties

    public string Text => GetName();

    #endregion

    #region IKeywordAction Member

    public void Execute (string keyword, string param, ILogExpertCallback callback, ILogLineColumnizer columnizer)
    {
        FormCollection openForms = Application.OpenForms;
        foreach (Form form in openForms)
        {
            if (form.TopLevel && form.Name.Equals("LogTabWindow", StringComparison.OrdinalIgnoreCase) && form.Text.Contains(callback.GetFileName(), StringComparison.Ordinal))
            {
                form.BeginInvoke(FlashWindow, [form]);
            }
        }
    }

    private void FlashWindow (Form form)
    {
        FLASHWINFO fw = new()
        {
            cbSize = Convert.ToUInt32(Marshal.SizeOf(typeof(FLASHWINFO))),
            hwnd = form.Handle,
            dwFlags = 14,
            uCount = 0
        };

        Win32Stuff.FlashWindowEx(ref fw);
    }

    public string GetDescription ()
    {
        return "Let the taskbar icon flash ";
    }

    public string GetName ()
    {
        return "Flash Icon";
    }

    #endregion
}