using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace LogExpert.UI.Extensions;

/// <summary>
/// Hardened clipboard writes. Windows clipboard access fails with an
/// <see cref="ExternalException"/> when another application holds the clipboard open
/// (clipboard managers, RDP, Office, ...) — even after the WinForms-internal retries.
/// These helpers report failure instead of letting the exception crash the UI.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class ClipboardHelper
{
    public static bool TrySetText (string text)
    {
        try
        {
            Clipboard.SetText(text);
            return true;
        }
        catch (ExternalException)
        {
            return false;
        }
    }

    public static bool TrySetDataObject (object data)
    {
        try
        {
            // copy: true keeps the data available after LogExpert exits (same as SetText)
            Clipboard.SetDataObject(data, copy: true);
            return true;
        }
        catch (ExternalException)
        {
            return false;
        }
    }
}
