using System.CommandLine;
using System.Diagnostics;
using System.Globalization;
using System.IO.Pipes;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;

using LogExpert.Classes;
using LogExpert.Configuration;
using LogExpert.Core.Classes.IPC;
using LogExpert.Core.Config;
using LogExpert.Core.Interfaces;
using LogExpert.PluginRegistry;
using LogExpert.UI.Dialogs;
using LogExpert.UI.Extensions.LogWindow;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NLog;

namespace LogExpert;

internal static class Program
{
    #region Fields

    private static readonly Logger _logger = LogManager.GetLogger("Program");
    private const string PIPE_SERVER_NAME = "LogExpert_IPC";
    private const int PIPE_CONNECTION_TIMEOUT_IN_MS = 5000;

    #endregion

    #region Private Methods

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    [SupportedOSPlatform("windows")]
    private static void Main (string[] args)
    {
        // Set global regex timeout to prevent DoS attacks from catastrophic backtracking
        AppDomain.CurrentDomain.SetData("REGEX_DEFAULT_MATCH_TIMEOUT", TimeSpan.FromSeconds(2));

        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        Application.ThreadException += Application_ThreadException;

        ApplicationConfiguration.Initialize();

        Application.EnableVisualStyles();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

        // Register the plugin assembly resolver early so that settings deserialization
        // can find plugin types (e.g., CsvColumnizer) before PluginRegistry.Create() runs.
        PluginRegistry.PluginRegistry.RegisterAssemblyResolver();

        // Initialize ConfigManager with application-specific paths and screen information
        ConfigManager.Instance.Initialize(Application.StartupPath, SystemInformation.VirtualScreen);
        PluginValidator.Initialize(ConfigManager.Instance.ActiveConfigDir);

        _logger.Info(CultureInfo.InvariantCulture, $"\r\n============================================================================\r\nLogExpert {Assembly.GetExecutingAssembly().GetName().Version.ToString(3)} started.\r\n============================================================================");

        CancellationTokenSource cts = new();
        try
        {
            var options = new CommandLineOptions();
            ParseResult parseResult = options.Parse(args);

            if (parseResult.Errors.Count > 0)
            {
                string errorText = string.Join(Environment.NewLine, parseResult.Errors.Select(e => e.Message));
                _logger.Error(CultureInfo.InvariantCulture, $"Command-line error: {errorText}");
                _ = MessageBox.Show(errorText, Resources.LogExpert_Common_UI_Title_LogExpert, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            FileInfo? configFile = parseResult.GetValue(options.ConfigOption) ?? parseResult.GetValue(options.LegacyConfigOption);

            if (configFile is not null)
            {
                if (configFile.Exists)
                {
                    ImportResult importResult = ConfigManager.Instance.Import(configFile, ExportImportFlags.All);

                    if (!importResult.Success)
                    {
                        string message = importResult.RequiresUserConfirmation
                            ? importResult.ConfirmationMessage
                            : importResult.ErrorMessage;
                        string title = importResult.RequiresUserConfirmation
                            ? importResult.ConfirmationTitle
                            : importResult.ErrorTitle;

                        if (MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                        {
                            _logger.Warn(CultureInfo.InvariantCulture, "### Program: Import of config file cancelled by user.");
                            Application.Exit();
                            return;
                        }
                    }
                }
                else
                {
                    _ = MessageBox.Show(Resources.Program_UI_Error_ConfigFileNotFound, Resources.LogExpert_Common_UI_Title_LogExpert);
                }
            }

            _ = PluginRegistry.PluginRegistry.Create(ConfigManager.Instance.ActiveConfigDir, ConfigManager.Instance.Settings.Preferences.PollingInterval);

            SetCulture();
            SetColorMode();

            ColumnizerLib.Column.SetMaxDisplayLength(ConfigManager.Instance.Settings.Preferences.MaxDisplayLength);

            var pId = Process.GetCurrentProcess().SessionId;

            try
            {
                Mutex mutex = new(false, "Local\\LogExpertInstanceMutex" + pId, out var isCreated);
                string[] positionalFiles = parseResult.GetValue(options.FilesArgument) ?? [];
                var absoluteFilePaths = GenerateAbsoluteFilePaths(positionalFiles);
                var targetLine = parseResult.GetValue(options.LineOption);

                if (isCreated)
                {
                    // first application instance
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    var logWin = AbstractLogTabWindow.Create(
                        absoluteFilePaths.Length > 0
                            ? absoluteFilePaths
                            : null,
                        1,
                        false,
                        ConfigManager.Instance,
                        targetLine);

                    // first instance
                    var wi = WindowsIdentity.GetCurrent();
                    LogExpertProxy proxy = new(logWin);
                    LogExpertApplicationContext context = new(proxy, logWin);

                    _ = Task.Run(() => RunServerLoopAsync(SendMessageToProxy, proxy, cts.Token));

                    Application.Run(context);
                }
                else
                {
                    var counter = 3;
                    Exception errMsg = null;
                    bool ipcSucceeded = false;

                    var settings = ConfigManager.Instance.Settings;
                    while (counter > 0)
                    {
                        try
                        {
                            var wi = WindowsIdentity.GetCurrent();
                            var command = SerializeCommandIntoNonFormattedJSON(absoluteFilePaths, settings.Preferences.AllowOnlyOneInstance, targetLine);
                            SendCommandToServer(command);
                            ipcSucceeded = true;
                            break;
                        }
                        catch (Exception ex) when (ex is ArgumentNullException
                                                       or ArgumentOutOfRangeException
                                                       or ArgumentException
                                                       or SecurityException)
                        {
                            _logger.Error($"IpcClientChannel error: {ex}");
                            errMsg = ex;
                            counter--;

                            if (counter > 0)
                            {
                                Task.Delay(500).Wait();
                            }
                        }
                    }

                    // Handle IPC failure
                    if (!ipcSucceeded)
                    {
                        _logger.Error($"IpcClientChannel error, giving up: {errMsg}");

                        // Show error, then create new instance (fallback)
                        _ = MessageBox.Show(
                            string.Format(CultureInfo.InvariantCulture, Resources.Program_UI_Error_Pipe_CannotConnectToFirstInstance, errMsg),
                            Resources.LogExpert_Common_UI_Title_LogExpert,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

                        _logger.Warn("IPC failed, creating new instance as fallback");
                        // Fall through to create new instance
                    }
                    else
                    {
                        // IPC succeeded - exit this instance
                        _logger.Info("Files sent to existing instance via IPC, exiting");
                        mutex.Close();
                        cts.Cancel();
                        return;
                    }
                }

                mutex.Close();
                cts.Cancel();
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException
                                       or IOException
                                       or DirectoryNotFoundException
                                       or PathTooLongException
                                       or WaitHandleCannotBeOpenedException
                                       or InvalidOperationException
                                       or SecurityException
                                       or ArgumentNullException
                                       or ArgumentException)
            {
                _logger.Error($"Mutex error, giving up: {ex}");
                cts.Cancel();
                _ = MessageBox.Show(string.Format(CultureInfo.InvariantCulture, Resources.Program_UI_Error_Pipe_CannotConnectToFirstInstance, ex.Message), Resources.LogExpert_Common_UI_Title_LogExpert);
            }
        }
        catch (SecurityException se)
        {
            _ = MessageBox.Show(string.Format(CultureInfo.InvariantCulture, Resources.Program_UI_Error_InsufficientRights, se.Message), Resources.LogExpert_Common_UI_Title_Error);
            cts.Cancel();
        }
    }

    [SupportedOSPlatform("windows")]
    private static void SetColorMode ()
    {
        Application.SetColorMode(ConfigManager.Instance.Settings.Preferences.ColorMode switch
        {
            ColorMode.Dark => SystemColorMode.Dark,
            ColorMode.System => SystemColorMode.System,
            _ => SystemColorMode.Classic,
        });
    }

    [SupportedOSPlatform("windows")]
    private static void SetCulture ()
    {
        var defaultCulture = CultureInfo.GetCultureInfo(ConfigManager.Instance.Settings.Preferences.DefaultLanguage ?? "en-US");

        CultureInfo.CurrentUICulture = defaultCulture;
        CultureInfo.CurrentCulture = defaultCulture;
    }

    internal static string SerializeCommandIntoNonFormattedJSON (string[] fileNames, bool allowOnlyOneInstance, int? targetLine = null)
    {
        var message = new IpcMessage()
        {
            Type = allowOnlyOneInstance ? IpcMessageType.NewWindowOrLockedWindow : IpcMessageType.NewWindow,
            Payload = JObject.FromObject(new LoadPayload { Files = [.. fileNames], TargetLine = targetLine })
        };

        return JsonConvert.SerializeObject(message, Formatting.None);
    }

    // This loop tries to convert relative file names into absolute file names (assuming that platform file names are given).
    // It tolerates errors, to give file system plugins (e.g. sftp) a change later.
    // TODO: possibly should be moved to LocalFileSystem plugin
    private static string[] GenerateAbsoluteFilePaths (string[] remainingArgs)
    {
        List<string> argsList = [];

        foreach (var fileArg in remainingArgs)
        {
            try
            {
                FileInfo info = new(fileArg);
                argsList.Add(info.Exists ? info.FullName : fileArg);
            }
            catch (Exception ex) when (ex is ArgumentNullException
                                        or SecurityException
                                        or ArgumentException
                                        or UnauthorizedAccessException
                                        or PathTooLongException
                                        or NotSupportedException)
            {
                argsList.Add(fileArg);
            }
        }

        return [.. argsList];
    }

    [SupportedOSPlatform("windows")]
    internal static void SendMessageToProxy (IpcMessage message, ILogExpertProxy proxy)
    {
        LoadPayload? payLoad;
        try
        {
            var target = message?.Payload?.GetValue(nameof(LoadPayload.TargetLine), StringComparison.OrdinalIgnoreCase);
            if (target != null && target.Type is not JTokenType.Integer and not JTokenType.Null)
            {
                _logger.Error("Invalid IPC target: the line number must be an integer.");
                return;
            }

            payLoad = message?.Payload?.ToObject<LoadPayload>();
        }
        catch (JsonException ex)
        {
            _logger.Error(ex, "Invalid IPC load payload.");
            return;
        }

        if (CheckPayload(payLoad))
        {
            switch (message.Type)
            {
                case IpcMessageType.Load:
                    proxy.LoadFiles([.. payLoad.Files], payLoad.TargetLine);
                    break;
                case IpcMessageType.NewWindow:
                    proxy.NewWindow([.. payLoad.Files], payLoad.TargetLine);
                    break;
                case IpcMessageType.NewWindowOrLockedWindow:
                    proxy.NewWindowOrLockedWindow([.. payLoad.Files], payLoad.TargetLine);
                    break;
                default:
                    _logger.Error($"Unknown IPC Message Type: {message.Type} with payload: {payLoad}");
                    break;
            }
        }
    }

    private static bool CheckPayload (LoadPayload payLoad)
    {
        if (payLoad == null)
        {
            _logger.Error("Invalid payload command: null");
            return false;
        }

        if (payLoad.GetValidationError() is string error)
        {
            _logger.Error($"Invalid IPC load payload: {error}");
            return false;
        }

        return true;
    }

    private static void SendCommandToServer (string command)
    {
        using var client = new NamedPipeClientStream(".", PIPE_SERVER_NAME, PipeDirection.Out);

        try
        {
            client.Connect(PIPE_CONNECTION_TIMEOUT_IN_MS);
        }
        catch (TimeoutException)
        {
            _logger.Error("Timeout connecting to pipe server");
            return;
        }
        catch (IOException ex)
        {
            _logger.Warn($"An I/O error occurred while connecting to the pipe server: {ex}");
            return;
        }
        catch (InvalidOperationException ioe)
        {
            _logger.Warn($"Invalid Operation while connecting to the pipe server: {ioe}");
            return;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.Warn($"Unauthorized access while connecting to the pipe server: {ex}");
            return;
        }

        using var writer = new StreamWriter(client, Encoding.UTF8) { AutoFlush = true };
        writer.WriteLine(command);
    }

    [SupportedOSPlatform("windows")]
    private static async Task RunServerLoopAsync (Action<IpcMessage, LogExpertProxy> onCommand, LogExpertProxy proxy, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            using var server = new NamedPipeServerStream(
                PIPE_SERVER_NAME,
                PipeDirection.In,
                1,
                PipeTransmissionMode.Message,
                PipeOptions.Asynchronous);

            try
            {
                await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
                using var reader = new StreamReader(server, Encoding.UTF8);
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);

                if (line != null)
                {
                    var message = JsonConvert.DeserializeObject<IpcMessage>(line);
                    onCommand(message, proxy);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex) when (ex is IOException
                                               or ObjectDisposedException
                                               or TimeoutException
                                               or InvalidOperationException
                                               or UnauthorizedAccessException
                                               or SecurityException
                                               or ArgumentNullException
                                               or ArgumentOutOfRangeException
                                               or ArgumentException)
            {
                _logger.Warn($"Pipe server error: {ex}");
            }
        }
    }

    [STAThread]
    [SupportedOSPlatform("windows")]
    private static void ShowUnhandledException (object exceptionObject)
    {
        var errorText = string.Empty;
        string stackTrace;

        if (exceptionObject is Exception exception)
        {
            errorText = exception.Message;
            stackTrace = $"\r\n{exception.GetType().Name}\r\n{exception.StackTrace}";
        }
        else
        {
            stackTrace = exceptionObject.ToString();
            var lines = stackTrace.Split('\n');

            if (lines != null && lines.Length > 0)
            {
                errorText = lines[0];
            }
        }

        ExceptionWindow win = new(errorText, stackTrace);
        _ = win.ShowDialog();
    }

    #endregion

    #region Events handler

    [SupportedOSPlatform("windows")]
    private static void Application_ThreadException (object sender, ThreadExceptionEventArgs e)
    {
        _logger.Fatal(e);

        Thread thread = new(ShowUnhandledException)
        {
            IsBackground = true
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start(e.Exception);
        thread.Join();
    }

    [SupportedOSPlatform("windows")]
    private static void CurrentDomain_UnhandledException (object sender, UnhandledExceptionEventArgs e)
    {
        _logger.Fatal(e);

        var exceptionObject = e.ExceptionObject;

        Thread thread = new(ShowUnhandledException)
        {
            IsBackground = true
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start(exceptionObject);
        thread.Join();
    }

    #endregion
}