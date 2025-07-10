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
using LogExpert.Classes.CommandLine;
using LogExpert.Config;
using LogExpert.Core.Classes.IPC;
using LogExpert.Core.Config;
using LogExpert.Dialogs;
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
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        Application.ThreadException += Application_ThreadException;

        ApplicationConfiguration.Initialize();

        Application.EnableVisualStyles();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

        _logger.Info(CultureInfo.InvariantCulture, $"\r\n============================================================================\r\nLogExpert {Assembly.GetExecutingAssembly().GetName().Version.ToString(3)} started.\r\n============================================================================");

        CancellationTokenSource cts = new();
        try
        {
            CmdLineString configFile = new("config", false, "A configuration (settings) file");
            CmdLine cmdLine = new();
            cmdLine.RegisterParameter(configFile);
            if (configFile.Exists)
            {
                FileInfo cfgFileInfo = new(configFile.Value);
                //TODO: The config file import and the try catch for the primary instance and secondary instance should be separated functions
                if (cfgFileInfo.Exists)
                {
                    ConfigManager.Instance.Import(cfgFileInfo, ExportImportFlags.All);
                }
                else
                {
                    _ = MessageBox.Show(Resources.Program_UI_Error_ConfigFileNotFound, Resources.Title_LogExpert);
                }
            }

            SetCulture();

            _ = PluginRegistry.PluginRegistry.Instance.Create(ConfigManager.Instance.ConfigDir, ConfigManager.Instance.Settings.Preferences.PollingInterval);

            var pId = Process.GetCurrentProcess().SessionId;

            try
            {
                Mutex mutex = new(false, "Local\\LogExpertInstanceMutex" + pId, out var isCreated);
                var remainingArgs = cmdLine.Parse(args);
                var absoluteFilePaths = GenerateAbsoluteFilePaths(remainingArgs);

                if (isCreated)
                {
                    // first application instance
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    var logWin = AbstractLogTabWindow.Create(absoluteFilePaths.Length > 0 ? absoluteFilePaths : null, 1, false, ConfigManager.Instance);

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

                    var settings = ConfigManager.Instance.Settings;
                    while (counter > 0)
                    {
                        try
                        {
                            var wi = WindowsIdentity.GetCurrent();
                            var command = SerializeCommandIntoNonFormattedJSON(absoluteFilePaths, settings.Preferences.AllowOnlyOneInstance);
                            SendCommandToServer(command);
                            break;
                        }
                        catch (Exception ex) when (ex is ArgumentNullException
                                                       or ArgumentOutOfRangeException
                                                       or ArgumentException
                                                       or SecurityException)
                        {
                            _logger.Error(string.Format(CultureInfo.InvariantCulture, Resources.Program_Logger_Error_IPCChannel_ClientError_Default, ex));
                            errMsg = ex;
                            counter--;
                            Thread.Sleep(500);
                        }
                    }

                    if (counter == 0)
                    {
                        _logger.Error(string.Format(CultureInfo.InvariantCulture, Resources.Program_Logger_Error_IPCChannel_ClientError, errMsg));
                        _ = MessageBox.Show(string.Format(CultureInfo.InvariantCulture, Resources.Program_UI_Error_Pipe_CannotConnectToFirstInstance, errMsg), Resources.Title_LogExpert);
                    }

                    //Dont create a new separated instance of LogExpert if the settings allows only one instance
                    if (settings.Preferences.AllowOnlyOneInstance && settings.Preferences.ShowErrorMessageAllowOnlyOneInstances)
                    {
                        AllowOnlyOneInstanceErrorDialog a = new();
                        if (a.ShowDialog() == DialogResult.OK)
                        {
                            settings.Preferences.ShowErrorMessageAllowOnlyOneInstances = !a.DoNotShowThisMessageAgain;
                            ConfigManager.Instance.Save(SettingsFlags.All);
                        }
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
                _logger.Error(string.Format(CultureInfo.InvariantCulture, Resources.Program_Logger_Error_MutexError, ex));
                cts.Cancel();
                _ = MessageBox.Show(string.Format(CultureInfo.InvariantCulture, Resources.Program_UI_Error_Pipe_CannotConnectToFirstInstance, ex.Message), Resources.Title_LogExpert);
            }
        }
        catch (SecurityException se)
        {
            _ = MessageBox.Show(string.Format(CultureInfo.InvariantCulture, Resources.Program_UI_Error_InsufficientRights, se.Message), Resources.Title_LogExpert_Error);
            cts.Cancel();
        }
    }

    [SupportedOSPlatform("windows")]
    private static void SetCulture ()
    {
        var defaultCulture = CultureInfo.GetCultureInfo(ConfigManager.Instance.Settings.Preferences.DefaultLanguage ?? "en-US");

        CultureInfo.CurrentUICulture = defaultCulture;
        CultureInfo.CurrentCulture = defaultCulture;
    }

    private static string SerializeCommandIntoNonFormattedJSON (string[] fileNames, bool allowOnlyOneInstance)
    {
        var message = new IpcMessage()
        {
            Type = allowOnlyOneInstance ? IpcMessageType.NewWindowOrLockedWindow : IpcMessageType.NewWindow,
            Payload = JObject.FromObject(new LoadPayload { Files = [.. fileNames] })
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
    private static void SendMessageToProxy (IpcMessage message, LogExpertProxy proxy)
    {
        var payLoad = message.Payload.ToObject<LoadPayload>();

        if (CheckPayload(payLoad))
        {
            switch (message.Type)
            {
                case IpcMessageType.Load:
                    proxy.LoadFiles([.. payLoad.Files]);
                    break;
                case IpcMessageType.NewWindow:
                    proxy.NewWindow([.. payLoad.Files]);
                    break;
                case IpcMessageType.NewWindowOrLockedWindow:
                    proxy.NewWindowOrLockedWindow([.. payLoad.Files]);
                    break;
                default:
                    _logger.Error(string.Format(CultureInfo.InvariantCulture, Resources.Program_Logger_Error_Pipe_UnknownIPCMessage, message.Type, payLoad));
                    break;
            }
        }
    }

    private static bool CheckPayload (LoadPayload payLoad)
    {
        if (payLoad == null)
        {
            _logger.Error(Resources.Program_Logger_Error_Payload_InvalidCommand);
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
            _logger.Error(Resources.Program_Logger_Error_Pipe_TimeoutException);
            return;
        }
        catch (IOException ex)
        {
            _logger.Warn(string.Format(CultureInfo.InvariantCulture, Resources.Program_Logger_Warn_Error_Pipe_IOException, ex));
            return;
        }
        catch (InvalidOperationException ioe)
        {
            _logger.Warn(string.Format(CultureInfo.InvariantCulture, Resources.Program_Logger_Warn_Pipe_InvalidOperationException, ioe));
            return;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.Warn(string.Format(CultureInfo.InvariantCulture, Resources.Program_Logger_Warn_Pipe_UnauthorizedAccessException, ex));
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
                _logger.Warn(string.Format(CultureInfo.InvariantCulture, Resources.Program_Logger_Warn_Pipe_CommonError, ex));
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
