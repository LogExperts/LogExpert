using ColumnizerLib;

using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;

namespace SftpFileSystem;

public class SftpLogFileInfo : ILogFileInfo
{
    #region Static/Constants
    //TODO Add to Options
    private const int RETRY_COUNT = 20;
    private const int RETRY_SLEEP = 250;

    #endregion

    #region Private Fields

    private readonly ILogExpertLogger _logger;
    private readonly string _remoteFileName;

    private readonly SftpClient _sftp;
    private readonly object _sshKeyMonitor = new();
    private DateTime _lastChange = DateTime.Now;
    private long _lastLength;

    #endregion

    #region Ctor

    public SftpLogFileInfo (SftpFileSystem sftpFileSystem, Uri fileUri, ILogExpertLogger logger)
    {
        _logger = logger;
        var sftFileSystem = sftpFileSystem;
        Uri = fileUri;
        _remoteFileName = Uri.PathAndQuery;

        var port = Uri.Port != -1 ? Uri.Port : 22;

        var success = false;
        var cancelled = false;
        if (sftFileSystem.ConfigData.UseKeyfile)
        {
            lock (_sshKeyMonitor) // prevent multiple password dialogs when opening multiple files at once
            {
                while (sftFileSystem.PrivateKeyFile == null)
                {
                    PrivateKeyPasswordDialog dlg = new();
                    var dialogResult = dlg.ShowDialog();
                    if (dialogResult == DialogResult.Cancel)
                    {
                        cancelled = true;
                        break;
                    }

                    PrivateKeyFile privateKeyFile = null;
                    
                    try
                    {
                        privateKeyFile = new(sftFileSystem.ConfigData.KeyFile, dlg.Password);
                    }
                    catch (ArgumentNullException ex)
                    {
                        _logger.LogError(ex.Message);
                    }
                    
                    if (privateKeyFile != null)
                    {
                        sftFileSystem.PrivateKeyFile = privateKeyFile;
                    }
                    else
                    {
                        _ = MessageBox.Show("Loading key file failed");
                    }
                }
            }

            if (!cancelled)
            {
                success = false;
                var credentials = sftFileSystem.GetCredentials(Uri, true, true);
                while (!success)
                {
                    //Add ConnectionInfo object
                    try
                    { 
                        _sftp = new SftpClient(Uri.Host, credentials.UserName, sftFileSystem.PrivateKeyFile);
                    }
                    catch (Exception ex) when(ex is ArgumentException or
                                       ArgumentNullException or
                                       ArgumentOutOfRangeException)
                    {
                        _logger.LogError(ex.Message);
                    }

                    if (_sftp != null)
                    {
                        _sftp.Connect();
                        success = true;
                    }

                    if (!success)
                    {
                        FailedKeyDialog dlg = new();
                        var res = dlg.ShowDialog();
                        dlg.Dispose();
                        if (res == DialogResult.Cancel)
                        {
                            return;
                        }

                        if (res == DialogResult.OK)
                        {
                            break; // go to user/pw auth
                        }

                        // retries with disabled cache
                        credentials = sftFileSystem.GetCredentials(Uri, false, true);
                    }
                }
            }
        }

        if (!success)
        {
            // username/password auth
            var credentials = sftFileSystem.GetCredentials(Uri, true, false);
            try
            {
                _sftp = new SftpClient(Uri.Host, port, credentials.UserName, credentials.Password);
            }
            catch (Exception ex) when (ex is ArgumentException or
                                       ArgumentNullException or
                                       ArgumentOutOfRangeException)
            {
               _logger.LogError(ex.Message);
            }
            
            if (_sftp == null)
            {
                try
                {
                    // first fail -> try again with disabled cache
                    credentials = sftFileSystem.GetCredentials(Uri, false, false);
                    _sftp = new SftpClient(Uri.Host, port, credentials.UserName, credentials.Password);
                }
                catch (Exception ex) when (ex is ArgumentException or
                                           ArgumentNullException or
                                           ArgumentOutOfRangeException)
                {
                    _logger.LogError(ex.Message);
                }

                if (_sftp == null)
                {
                    // 2nd fail -> abort
                    _ = MessageBox.Show("Authentication failed!");
                    //MessageBox.Show(sftp.LastErrorText);
                    return;
                }
            }
            else
            {
                _sftp.Connect();
            }
        }

        if (!_sftp.IsConnected)
        {
            _ = MessageBox.Show("Sftp is not connected");
            return;
        }

        try
        {
            OriginalLength = _lastLength = Length;
        }
        catch (Exception e) when (e is SftpPathNotFoundException or
                                       SftpPermissionDeniedException)
        {
            _logger.LogError(e.Message);
            OriginalLength = _lastLength = -1;
        }
    }

    #endregion

    #region Interface ILogFileInfo

    public string DirectoryName
    {
        get
        {
            var full = FullName;
            var i = full.LastIndexOf(DirectorySeparatorChar);
            return i != -1
                ? full[..i]
                : ".";
        }
    }

    public char DirectorySeparatorChar => '/';

    public bool FileExists
    {
        get
        {
            try
            {
                var file = (SftpFile)_sftp.Get(_remoteFileName);
                var len = file.Attributes.Size;
                return len != -1;
            }
            catch (Exception e) when (e is SshException or
                                           SftpPathNotFoundException or
                                           SftpPermissionDeniedException or
                                           ArgumentNullException)
            {
                _logger.LogError(e.Message);
                return false;
            }
        }
    }

    public string FileName
    {
        get
        {
            var full = FullName;
            var i = full.LastIndexOf(DirectorySeparatorChar);
            return full[(i + 1)..];
        }
    }

    public string FullName => Uri.ToString();

    public DateTime? LastWriteTimeUtc
    {
        get
        {
            try
            {
                var file = (SftpFile)_sftp.Get(_remoteFileName);
                return file.Attributes.LastWriteTimeUtc;
            }
            catch (Exception e) when (e is SshException or
                                           SftpPathNotFoundException or
                                           SftpPermissionDeniedException or
                                           ArgumentNullException)
            {
                _logger.LogError(e.Message);
                return null;
            }
        }
    }

    public long Length
    {
        get
        {
            try
            {
                var file = (SftpFile)_sftp.Get(_remoteFileName);
                return file.Attributes.Size;
            }
            catch (Exception e) when (e is SftpPathNotFoundException or
                                           SftpPermissionDeniedException)
            {
                _logger.LogError(e.Message);
                return -1;
            }
        }
    }

    public long OriginalLength { get; }

    public int PollInterval
    {
        get
        {
            var diff = DateTime.Now - _lastChange;
            return diff.TotalSeconds < 4
                ? 400
                : diff.TotalSeconds < 30
                    ? (int)diff.TotalSeconds * 100
                    : 5000;
        }
    }

    public Uri Uri { get; }

    public bool FileHasChanged ()
    {
        if (Length != _lastLength)
        {
            _lastLength = Length;
            _lastChange = DateTime.Now;
            return true;
        }

        return false;
    }

    public Stream OpenStream ()
    {
        var retry = RETRY_COUNT;
        while (true)
        {
            try
            {
                return _sftp.OpenRead(_remoteFileName);
            }
            catch (IOException)
            {
                //First remove a try then check if its less or 0
                if (--retry <= 0)
                {
                    throw;
                }

                Thread.Sleep(RETRY_SLEEP);
            }
        }
    }

    #endregion
}
