using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;

using LogExpert.Core.Classes;
using LogExpert.Core.Classes.Columnizer;
using LogExpert.Core.Interfaces;
using LogExpert.UI.Interface;

using NLog;

namespace LogExpert.UI.Services.ToolLaunchService;

[SupportedOSPlatform("windows")]
internal sealed class ToolLaunchService (IPluginRegistry pluginRegistry) : IToolLaunchService
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public ToolLaunchResult Launch (ToolLaunchRequest request)
    {
        return string.IsNullOrEmpty(request.Cmd)
            ? new ToolLaunchResult
            {
                HasError = true,
                ErrorMessage = "Command must not be empty."
            }
            : request.SysoutPipe
                ? LaunchWithSysoutPipe(request)
                : LaunchExternal(request);
    }

    private static ToolLaunchResult LaunchExternal (ToolLaunchRequest request)
    {
        var startInfo = BuildStartInfo(request);

        startInfo.UseShellExecute = false;

        (bool flowControl, ToolLaunchResult value, _) = LaunchProcess(startInfo);

        return !flowControl
            ? value
            : new ToolLaunchResult { HasError = false };
    }

    private static (bool flowControl, ToolLaunchResult value, Process process) LaunchProcess (ProcessStartInfo startInfo)
    {
        Process process = new() { StartInfo = startInfo, EnableRaisingEvents = true };

        try
        {
            _ = process.Start();
        }
        catch (Exception e) when (e is Win32Exception or
                                       InvalidOperationException or
                                       ObjectDisposedException or
                                       PlatformNotSupportedException)
        {
            _logger.Error(e);
            return (false, new ToolLaunchResult { HasError = true, ErrorMessage = e.Message }, process);
        }

        return (true, default, process);
    }

    private ToolLaunchResult LaunchWithSysoutPipe (ToolLaunchRequest request)
    {
        var columnizer = string.IsNullOrEmpty(request.ColumnizerName)
            ? null
            : ColumnizerPicker.DecideMemoryColumnizerByName(request.ColumnizerName, pluginRegistry.RegisteredColumnizers);

        var startInfo = BuildStartInfo(request);
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;

        (bool flowControl, ToolLaunchResult value, Process process) = LaunchProcess(startInfo);

        if (!flowControl)
        {
            return value;
        }

        // TODO: SysoutPipe temp file is never deleted — fire-and-forget lifetime by design.
        // SysoutPipe takes ownership of the process (keeps it alive, reads StandardOutput,
        // subscribes to Exited, and disposes it once output is drained).
        SysoutPipe pipe = new(process);

        return new ToolLaunchResult
        {
            HasError = false,
            PipeFileName = pipe.FileName,
            Columnizer = columnizer
        };
    }

    private static ProcessStartInfo BuildStartInfo (ToolLaunchRequest request)
    {
        var startInfo = new ProcessStartInfo(request.Cmd, request.Args);

        if (!string.IsNullOrEmpty(request.WorkingDir))
        {
            startInfo.WorkingDirectory = request.WorkingDir;
        }

        return startInfo;
    }
}
