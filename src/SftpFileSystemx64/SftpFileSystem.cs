using System.Globalization;
using System.Xml.Serialization;

using LogExpert;

using Renci.SshNet;

namespace SftpFileSystem;

public class SftpFileSystem (IFileSystemCallback callback) : IFileSystemPlugin, ILogExpertPluginConfigurator
{
    #region Private Fields

    private readonly ILogExpertLogger _logger = callback.GetLogger();

    private ConfigDialog _configDialog;
    private volatile PrivateKeyFile _privateKeyFile;
    private readonly object _lock = new();

    #endregion

    #region Interface IFileSystemPlugin

    public string Description => "Can read log files directly from SFTP server.";

    public string Text => "SFTP plugin";

    public bool CanHandleUri (string uriString)
    {
        try
        {
            Uri uri = new(uriString);
            return uri.Scheme.Equals("sftp", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            return false;
        }
    }

    public ILogFileInfo GetLogfileInfo (string uriString)
    {
        try
        {
            Uri uri = new(uriString.Replace('\\', '/'));
            return new SftpLogFileInfo(this, uri, _logger);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            return null;
        }
    }

    #endregion

    #region Interface ILogExpertPluginConfigurator

    public bool HasEmbeddedForm ()
    {
        return true;
    }

    public void HideConfigForm ()
    {
        ConfigData = _configDialog.ConfigData;
        _configDialog.Hide();
        _configDialog.Dispose();
    }

    //TODO JSON config should be used
    public void LoadConfig (string configDir)
    {
        XmlSerializer xml = new(ConfigData.GetType());

        FileInfo configFile = new(configDir + "\\" + "sftpfilesystem.cfg");

        if (!configFile.Exists)
        {
            return;
        }

        FileStream fs = null;

        try
        {
            fs = configFile.OpenRead();

            ConfigData = (ConfigData)xml.Deserialize(fs);
        }
        catch (IOException e)
        {
            _logger.LogError(e.Message);
        }
        finally
        {
            fs?.Flush();
            fs?.Close();
            fs?.Dispose();
        }
    }

    public void SaveConfig (string configDir)
    {
        _logger.Info(CultureInfo.InvariantCulture, "Saving SFTP config");
        XmlSerializer xml = new(ConfigData.GetType());

        FileStream fs = null;

        try
        {
            fs = new FileStream(configDir + "\\" + "sftpfilesystem.cfg", FileMode.Create);
            xml.Serialize(fs, ConfigData);
            fs.Close();
        }
        catch (IOException e)
        {
            _logger.LogError(e.Message);
        }
        finally
        {
            fs?.Flush();
            fs?.Close();
            fs?.Dispose();
        }
    }

    public void ShowConfigDialog (object owner)
    {
        throw new NotImplementedException();
    }

    public void ShowConfigForm (object parentPanel)
    {
        _configDialog = new ConfigDialog(ConfigData)
        {
            Parent = (Panel)parentPanel
        };

        _configDialog.Show();
    }

    public void StartConfig ()
    {
    }

    #endregion

    #region Properties / Indexers

    public ConfigData ConfigData { get; private set; } = new ConfigData();

    public PrivateKeyFile PrivateKeyFile
    {
        get => _privateKeyFile;
        set => _privateKeyFile = value;
    }

    private CredentialCache CredentialsCache { get; } = new CredentialCache();

    #endregion

    internal Credentials GetCredentials (Uri uri, bool cacheAllowed, bool hidePasswordField)
    {
        // Synchronized access to the GetCredentials() method prevents multiple login dialogs when loading multiple files at once
        // (e.g. on startup). So the user only needs to enter credentials once for the same host.
        lock (_lock)
        {
            string userName = null;
            string password = null;
            if (uri.UserInfo != null && uri.UserInfo.Length > 0)
            {
                var split = uri.UserInfo.Split(':');
                if (split.Length > 0)
                {
                    userName = split[0];
                }

                if (split.Length > 1)
                {
                    password = split[1];
                }
            }

            var usersForHost = CredentialsCache.GetUsersForHost(uri.Host);
            if (userName == null && cacheAllowed)
            {
                if (usersForHost.Count == 1)
                {
                    userName = usersForHost[0];
                }
            }

            if (userName != null && password == null && cacheAllowed)
            {
                var cred = CredentialsCache.GetCredentials(uri.Host, userName);
                if (cred != null)
                {
                    return cred;
                }
            }

            if (userName == null || password == null)
            {
                LoginDialog dlg = new(uri.Host, usersForHost, hidePasswordField)
                {
                    Username = userName
                };

                if (DialogResult.OK == dlg.ShowDialog())
                {
                    password = dlg.Password;
                    userName = dlg.Username;
                }

                dlg.Dispose();
            }

            Credentials credentials = new(uri.Host, userName, password);
            CredentialsCache.Add(credentials);
            return credentials;
        }
    }
}
