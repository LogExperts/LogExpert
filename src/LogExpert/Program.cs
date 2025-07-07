using LogExpert.Classes;
using LogExpert.Classes.CommandLine;
using LogExpert.Config;
using LogExpert.Core.Classes.IPC;
using LogExpert.Core.Config;
using LogExpert.Core.Interface;
using LogExpert.Dialogs;
using LogExpert.UI.Controls.LogWindow;
using LogExpert.UI.Dialogs;
using LogExpert.UI.Extensions.LogWindow;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NLog;

using System.Diagnostics;
using System.Globalization;
using System.IO.Pipes;
using System.Reflection;
using System.Security;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;

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
    private static void Main(string[] args)
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
                    MessageBox.Show(@"Config file not found", @"LogExpert");
                }
            }

            PluginRegistry.PluginRegistry.Instance.Create(ConfigManager.Instance.ConfigDir, ConfigManager.Instance.Settings.Preferences.PollingInterval);

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

                    Task.Run(() => RunServerLoopAsync(SendMessageToProxy, proxy, cts.Token));

                    Application.Run(context);
                }
                else
                {
                    var counter = 3;
                    Exception errMsg = null;

                    Settings settings = ConfigManager.Instance.Settings;
                    while (counter > 0)
                    {
                        try
                        {
                            var wi = WindowsIdentity.GetCurrent();
                            var command = SerializeCommandIntoNonFormattedJSON(absoluteFilePaths, settings.Preferences.AllowOnlyOneInstance);
                            SendCommandToServer(command);
                            break;
                        }
                        catch (Exception e)
                        {
                            _logger.Warn(e, "IpcClientChannel error: ");
                            errMsg = e;
                            counter--;
                            Thread.Sleep(500);
                        }
                    }

                    if (counter == 0)
                    {
                        _logger.Error(errMsg, "IpcClientChannel error, giving up: ");
                        MessageBox.Show($"Cannot open connection to first instance ({errMsg})", "LogExpert");
                    }

                    //TODO: Remove this from here? Why is it called from the Main project and not from the main window?
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
            catch (Exception ex)
            {
                _logger.Error(ex, "Mutex error, giving up: ");
                cts.Cancel();
                MessageBox.Show($"Cannot open connection to first instance ({ex.Message})", "LogExpert");
            }
        }
        catch (SecurityException se)
        {
            MessageBox.Show("Insufficient system rights for LogExpert. Maybe you have started it from a network drive. Please start LogExpert from a local drive.\n(" + se.Message + ")", "LogExpert Error");
            cts.Cancel();
        }
    }

    private static string SerializeCommandIntoNonFormattedJSON(string[] fileNames, bool allowOnlyOneInstance)
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
    private static string[] GenerateAbsoluteFilePaths(string[] remainingArgs)
    {
        List<string> argsList = [];

        foreach (var fileArg in remainingArgs)
        {
            try
            {
                FileInfo info = new(fileArg);
                argsList.Add(info.Exists ? info.FullName : fileArg);
            }
            catch (Exception)
            {
                argsList.Add(fileArg);
            }
        }

        return [.. argsList];
    }

    private static void SendMessageToProxy(IpcMessage message, LogExpertProxy proxy)
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
                    _logger.Error($"Unknown IPC Message Type: {message.Type}; with payload: {payLoad}");
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

        return true;
    }

    private static void SendCommandToServer(string command)
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
            _logger.Warn(ex, "An I/O error occurred while connecting to the pipe server.");
            return;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.Warn(ex, "Unauthorized access while connecting to the pipe server.");
            return;
        }

        using var writer = new StreamWriter(client, Encoding.UTF8) { AutoFlush = true };
        writer.WriteLine(command);
    }

    private static async Task RunServerLoopAsync(Action<IpcMessage, LogExpertProxy> onCommand, LogExpertProxy proxy, CancellationToken cancellationToken)
    {
        while (cancellationToken.IsCancellationRequested == false)
        {
            using var server = new NamedPipeServerStream(
                PIPE_SERVER_NAME,
                PipeDirection.In,
                1,
                PipeTransmissionMode.Message,
                PipeOptions.Asynchronous);

            try
            {
                await server.WaitForConnectionAsync(cancellationToken);
                using var reader = new StreamReader(server, Encoding.UTF8);
                var line = await reader.ReadLineAsync(cancellationToken);

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
            catch (Exception ex)
            {
                _logger.Warn(ex, "Pipe server error");
            }
        }
    }

    [STAThread]
    private static void ShowUnhandledException(object exceptionObject)
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

    private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
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

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
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
