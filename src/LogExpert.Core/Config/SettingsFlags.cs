using System;

namespace LogExpert.Core.Config;

[Flags]
public enum SettingsFlags : long
{
    None = 0,
    WindowPosition = 1,
    FileHistory = 2,
    HighlightSettings = 4,
    FilterList = 8,
    RegexHistory = 16,
    ToolSettings = 32,
    GuiOrColors = 64,
    FilterHistory = 128,

    All = WindowPosition | FileHistory | HighlightSettings |
          FilterList | RegexHistory | ToolSettings | GuiOrColors |
          FilterHistory,

    Settings = All & ~WindowPosition & ~FileHistory,
}