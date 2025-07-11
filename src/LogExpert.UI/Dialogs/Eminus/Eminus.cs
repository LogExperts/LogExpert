using System.Globalization;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Versioning;

//using System.Windows.Forms;
using System.Xml;

using Newtonsoft.Json;

//See Logexpert Help Eminus Plugin for more information
//[assembly: SupportedOSPlatform("windows")]
namespace LogExpert.UI.Dialogs.Eminus;

internal class Eminus : IContextMenuEntry, ILogExpertPluginConfigurator
{
    #region Fields

    private const string CFG_FILE_NAME = "eminus.json";
    private const string DOT = ".";
    private const string DOUBLE_DOT = ":";
    private const string DISABLED = "_";
    private const string AT = "at ";
    private const string CREATED_IN = "created in ";
    private const string NESTED = "Nested:";
    private const string EXCEPTION_OF_TYPE = "Exception of type";

    private EminusConfig _config = new();
    private EminusConfigDlg dlg;
    private EminusConfig tmpConfig = new();

    #endregion

    #region Properties

    public static string Text => "eminus";

    #endregion

    #region Private Methods

    [SupportedOSPlatform("windows")]
    private XmlDocument BuildParam (ILogLine line)
    {
        var fullLogLine = line.FullLine;
        // no Java stacktrace but some special logging of our applications at work:
        if (fullLogLine.Contains(EXCEPTION_OF_TYPE, StringComparison.CurrentCulture) ||
            fullLogLine.Contains(NESTED, StringComparison.CurrentCulture))
        {
            var pos = fullLogLine.IndexOf(CREATED_IN, StringComparison.OrdinalIgnoreCase);

            if (pos == -1)
            {
                return null;
            }

            pos += CREATED_IN.Length;
            var endPos = fullLogLine.IndexOf(DOT, pos, StringComparison.OrdinalIgnoreCase);

            if (endPos == -1)
            {
                return null;
            }

            var className = fullLogLine[pos..endPos];
            pos = fullLogLine.IndexOf(DOUBLE_DOT, pos, StringComparison.OrdinalIgnoreCase);

            if (pos == -1)
            {
                return null;
            }

            var lineNum = fullLogLine[(pos + 1)..];
            var doc = BuildXmlDocument(className, lineNum);
            return doc;
        }

        if (fullLogLine.Contains(AT, StringComparison.OrdinalIgnoreCase))
        {
            var str = fullLogLine.Trim();
            string className = null;
            string lineNum = null;
            var pos = str.IndexOf(AT, StringComparison.OrdinalIgnoreCase) + 3;
            str = str[pos..]; // remove 'at '
            var idx = str.IndexOfAny(['(', '$', '<']);

            if (idx != -1)
            {
                if (str[idx] == '$')
                {
                    className = str[..idx];
                }
                else
                {
                    pos = str.LastIndexOf(DOT, idx, StringComparison.OrdinalIgnoreCase);
                    if (pos == -1)
                    {
                        return null;
                    }

                    className = str[..pos];
                }

                idx = str.LastIndexOf(DOUBLE_DOT, StringComparison.OrdinalIgnoreCase);

                if (idx == -1)
                {
                    return null;
                }

                pos = str.IndexOf(')', idx);

                if (pos == -1)
                {
                    return null;
                }

                lineNum = str.Substring(idx + 1, pos - idx - 1);
            }
            /*
             * <?xml version="1.0" encoding="UTF-8"?>
                <loadclass>
                    <!-- full qualified java class name -->
                    <classname></classname>
                    <!-- line number one based -->
                    <linenumber></linenumber>
                </loadclass>
             */

            var doc = BuildXmlDocument(className, lineNum);
            return doc;
        }

        return null;
    }

    [SupportedOSPlatform("windows")]
    private XmlDocument BuildXmlDocument (string className, string lineNum)
    {
        XmlDocument xmlDoc = new();
        _ = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", "yes");
        var rootElement = xmlDoc.CreateElement("eminus");
        _ = xmlDoc.AppendChild(rootElement);
        rootElement.SetAttribute("authKey", _config.Password);

        var loadElement = xmlDoc.CreateElement("loadclass");
        loadElement.SetAttribute("mode", "dialog");
        _ = rootElement.AppendChild(loadElement);

        var elemClassName = xmlDoc.CreateElement("classname");
        var elemLineNum = xmlDoc.CreateElement("linenumber");
        elemClassName.InnerText = className;
        elemLineNum.InnerText = lineNum;
        _ = loadElement.AppendChild(elemClassName);
        _ = loadElement.AppendChild(elemLineNum);
        return xmlDoc;
    }

    #endregion

    #region IContextMenuEntry Member

    public string GetMenuText (IList<int> loglines, ILogLineColumnizer columnizer, ILogExpertCallback callback)
    {
        //not used
        return string.Empty;
    }

    [SupportedOSPlatform("windows")]
    public string GetMenuText (int linesCount, ILogLineColumnizer columnizer, ILogLine logline)
    {
        return linesCount == 1 && BuildParam(logline) != null
            ? Resources.Eminus_UI_GetMenuText_LoadClassInEclipse
            : string.Format(CultureInfo.InvariantCulture, Resources.Eminus_UI_GetMenuText_DISABLEDLoadClassInEclipse, DISABLED);
    }

    public void MenuSelected (IList<int> loglines, ILogLineColumnizer columnizer, ILogExpertCallback callback)
    {
        //Not used
    }

    [SupportedOSPlatform("windows")]
    public void MenuSelected (int linesCount, ILogLineColumnizer columnizer, ILogLine logline)
    {
        if (linesCount != 1)
        {
            return;
        }

        var doc = BuildParam(logline);

        if (doc == null)
        {
            _ = MessageBox.Show(Resources.Eminus_UI_CannotParseJavaStackTraceLine, Resources.Title_LogExpert);
        }
        else
        {
            try
            {
                using TcpClient client = new(_config.Host, _config.Port);
                using var stream = client.GetStream();
                using StreamWriter writer = new(stream);
                doc.Save(writer);
            }
            catch (Exception e) when (e is SocketException
                                        or ArgumentNullException
                                        or ArgumentOutOfRangeException
                                        or InvalidOperationException
                                        or ObjectDisposedException
                                        or XmlException)
            {
                _ = MessageBox.Show(e.Message, Resources.Title_LogExpert);
            }
        }
    }

    #endregion

    #region ILogExpertPluginConfigurator Member

    [SupportedOSPlatform("windows")]
    public void LoadConfig (string configDir)
    {
        var configPath = configDir + CFG_FILE_NAME;

        FileInfo fileInfo = new(configDir + Path.DirectorySeparatorChar + CFG_FILE_NAME);

        if (!File.Exists(configPath))
        {
            _config = new EminusConfig();
        }
        else
        {
            try
            {
                _config = JsonConvert.DeserializeObject<EminusConfig>(File.ReadAllText($"{fileInfo.FullName}"));
            }
            catch (SerializationException e)
            {
                _ = MessageBox.Show(e.Message, Resources.Title_LogExpert_Deserialize);
                _config = new EminusConfig();
            }
        }
    }

    [SupportedOSPlatform("windows")]
    public void SaveConfig (string configDir)
    {
        FileInfo fileInfo = new(configDir + Path.DirectorySeparatorChar + CFG_FILE_NAME);

        dlg?.ApplyChanges();

        _config = tmpConfig.Clone();

        using StreamWriter sw = new(fileInfo.Create());
        JsonSerializer serializer = new();
        serializer.Serialize(sw, _config);
    }

    public bool HasEmbeddedForm ()
    {
        return true;
    }

    [SupportedOSPlatform("windows")]
    public void ShowConfigForm (object parentPanel)
    {
        dlg = new EminusConfigDlg(tmpConfig)
        {
            Parent = (Panel)parentPanel
        };
        dlg.Show();
    }

    /// <summary>
    /// Implemented only for demonstration purposes. This function is called when the config button
    /// is pressed (HasEmbeddedForm() must return false for this).
    /// </summary>
    /// <param name="owner"></param>
    [SupportedOSPlatform("windows")]
    public void ShowConfigDialog (object owner)
    {
        dlg = new EminusConfigDlg(tmpConfig)
        {
            TopLevel = true,
            Owner = (Form)owner
        };

        _ = dlg.ShowDialog();
        dlg.ApplyChanges();
    }

    [SupportedOSPlatform("windows")]
    public void HideConfigForm ()
    {
        if (dlg != null)
        {
            dlg.ApplyChanges();
            dlg.Hide();
            dlg.Dispose();
            dlg = null;
        }
    }

    public void StartConfig ()
    {
        tmpConfig = _config.Clone();
    }

    #endregion
}