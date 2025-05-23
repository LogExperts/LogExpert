using LogExpert.Classes;
using LogExpert.Config;
using LogExpert.Controls.LogTabWindow;
using LogExpert.Core.Classes;
using LogExpert.Core.Classes.IPC;
using LogExpert.Core.Config;
using LogExpert.Dialogs;
using LogExpert.UI.Dialogs;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NLog;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Security;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LogExpert
{
    internal static class Program
    {
        #region Fields

        private static readonly Logger _logger = LogManager.GetLogger("Program");
        private const string PIPE_SERVER_NAME = "LogExpert_IPC";
        private const int PIPE_CONNECTION_TIMEOUT_IN_MS = 5000;
        private static readonly CancellationTokenSource _cts = new();

        #endregion

        #region Private Methods

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] orgArgs)
        {
            try
            {
                Sub_Main(orgArgs);
            }
            catch (SecurityException se)
            {
                MessageBox.Show("Insufficient system rights for LogExpert. Maybe you have started it from a network drive. Please start LogExpert from a local drive.\n(" + se.Message + ")", "LogExpert Error");
                _cts.Cancel();
            }
        }

        private static void Sub_Main(string[] orgArgs)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.ThreadException += Application_ThreadException;
            ApplicationConfiguration.Initialize();

            Application.EnableVisualStyles();
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            _logger.Info("\r\n============================================================================\r\nLogExpert {0} started.\r\n============================================================================", Assembly.GetExecutingAssembly().GetName().Version.ToString(3));

            CmdLine cmdLine = new();
            CmdLineString configFile = new("config", false, "A configuration (settings) file");
            cmdLine.RegisterParameter(configFile);
            string[] remainingArgs = cmdLine.Parse(orgArgs);
            string[] absoluteFilePaths = GenerateAbsoluteFilePaths(remainingArgs);

            if (configFile.Exists)
            {
                FileInfo cfgFileInfo = new(configFile.Value);

                if (cfgFileInfo.Exists)
                {
                    ConfigManager.Import(cfgFileInfo, ExportImportFlags.All);
                }
                else
                {
                    MessageBox.Show(@"Config file not found", @"LogExpert");
                }
            }

            PluginRegistry.PluginRegistry.Instance.Create(ConfigManager.ConfigDir, ConfigManager.Settings.Preferences.pollingInterval);

            int pId = Process.GetCurrentProcess().SessionId;

            try
            {
                Settings settings = ConfigManager.Settings;

                Mutex mutex = new(false, "Local\\LogExpertInstanceMutex" + pId, out var isCreated);

                if (isCreated)
                {
                    // first application instance
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    LogTabWindow logWin = new(absoluteFilePaths.Length > 0 ? absoluteFilePaths : null, 1, false);

                    // first instance
                    WindowsIdentity wi = WindowsIdentity.GetCurrent();
                    LogExpertProxy proxy = new(logWin);
                    LogExpertApplicationContext context = new(proxy, logWin);

                    Task.Run(() => RunServerLoopAsync(SendMessageToProxy, proxy, _cts.Token));

                    Application.Run(context);
                }
                else
                {
                    int counter = 3;
                    Exception errMsg = null;

                    while (counter > 0)
                    {
                        try
                        {
                            WindowsIdentity wi = WindowsIdentity.GetCurrent();
                            var command = SerializeCommandIntoNonFormattedJSON(absoluteFilePaths, settings.Preferences.allowOnlyOneInstance);
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

                    if (settings.Preferences.allowOnlyOneInstance && settings.Preferences.ShowErrorMessageAllowOnlyOneInstances)
                    {
                        AllowOnlyOneInstanceErrorDialog a = new();
                        if (a.ShowDialog() == DialogResult.OK)
                        {
                            settings.Preferences.ShowErrorMessageAllowOnlyOneInstances = !a.DoNotShowThisMessageAgain;
                            ConfigManager.Save(SettingsFlags.All);
                        }
                    }
                }

                mutex.Close();
                _cts.Cancel();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Mutex error, giving up: ");
                _cts.Cancel();
                MessageBox.Show($"Cannot open connection to first instance ({ex.Message})", "LogExpert");
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

            foreach (string fileArg in remainingArgs)
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
            switch (message.Type)
            {
                case IpcMessageType.Load:
                    {
                        var payLoad = message.Payload.ToObject<LoadPayload>();

                        if (CheckPayload(payLoad))
                        {
                            proxy.LoadFiles([.. payLoad.Files]);
                        }
                    }
                    break;
                case IpcMessageType.NewWindow:
                    {
                        var payLoad = message.Payload.ToObject<LoadPayload>();
                        if (CheckPayload(payLoad))
                        {
                            proxy.NewWindow([.. payLoad.Files]);
                        }
                    }
                    break;
                case IpcMessageType.NewWindowOrLockedWindow:
                    {
                        var payLoad = message.Payload.ToObject<LoadPayload>();
                        if (CheckPayload(payLoad))
                        {
                            proxy.NewWindowOrLockedWindow([.. payLoad.Files]);
                        }
                    }
                    break;
                default:
                    _logger.Error($"Unknown IPC Message Type {message.Type}");
                    break;
            }
        }

        private static bool CheckPayload(LoadPayload payLoad)
        {
            if (payLoad == null)
            {
                _logger.Error("Invalid payload for NewWindow command");
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
                    string line = await reader.ReadLineAsync(cancellationToken);

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
            string errorText = string.Empty;
            string stackTrace;

            if (exceptionObject is Exception exception)
            {
                errorText = exception.Message;
                stackTrace = $"\r\n{exception.GetType().Name}\r\n{exception.StackTrace}";
            }
            else
            {
                stackTrace = exceptionObject.ToString();
                string[] lines = stackTrace.Split('\n');

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

            object exceptionObject = e.ExceptionObject;

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
}
