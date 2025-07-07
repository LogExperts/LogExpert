using System.Runtime.Versioning;

using LogExpert.Core.Interface;
using LogExpert.UI.Controls.LogTabWindow;

namespace LogExpert.UI.Extensions.LogWindow;

public abstract class AbstractLogTabWindow ()
{
    public static StaticLogTabWindowData StaticData { get; set; } = new StaticLogTabWindowData();

    [SupportedOSPlatform("windows")]
    public static ILogTabWindow Create (string[] fileNames, int instanceNumber, bool showInstanceNumbers, IConfigManager configManager)
    {
        return new LogTabWindow(fileNames, instanceNumber, showInstanceNumbers, configManager);
    }
}