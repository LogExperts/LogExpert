using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Windows.Forms;
using System.Xml;

[assembly: SupportedOSPlatform("windows")]
namespace LogExpert
{
    public class Eminus : IContextMenuEntry, ILogExpertPluginConfigurator
    {
        #region Fields

        private const string CFG_FILE_NAME = "eminus.json";
        private const string DOT = ".";
        private const string DOUBLE_DOT = ":";
        private const string DISABLED = "_";

        private EminusConfig _config = new();
        private EminusConfigDlg dlg;
        private EminusConfig tmpConfig = new();

        #endregion

        #region Properties

        public static string Text => "eminus";

        #endregion

        #region Private Methods

        private XmlDocument BuildParam(ILogLine line)
        {
            string fullLogLine = line.FullLine;
            // no Java stacktrace but some special logging of our applications at work:
            if (fullLogLine.Contains("Exception of type", StringComparison.CurrentCulture) ||
                fullLogLine.Contains("Nested:", StringComparison.CurrentCulture))
            {
                int pos = fullLogLine.IndexOf("created in ");

                if (pos == -1)
                {
                    return null;
                }

                pos += "created in ".Length;
                int endPos = fullLogLine.IndexOf(DOT, pos);

                if (endPos == -1)
                {
                    return null;
                }

                string className = fullLogLine[pos..endPos];
                pos = fullLogLine.IndexOf(DOUBLE_DOT, pos);

                if (pos == -1)
                {
                    return null;
                }

                string lineNum = fullLogLine[(pos + 1)..];
                XmlDocument doc = BuildXmlDocument(className, lineNum);
                return doc;
            }

            if (fullLogLine.Contains("at ", StringComparison.CurrentCulture))
            {
                string str = fullLogLine.Trim();
                string className = null;
                string lineNum = null;
                int pos = str.IndexOf("at ") + 3;
                str = str[pos..]; // remove 'at '
                int idx = str.IndexOfAny(['(', '$', '<']);

                if (idx != -1)
                {
                    if (str[idx] == '$')
                    {
                        className = str[..idx];
                    }
                    else
                    {
                        pos = str.LastIndexOf(DOT, idx);
                        if (pos == -1)
                        {
                            return null;
                        }
                        className = str[..pos];
                    }

                    idx = str.LastIndexOf(DOUBLE_DOT);

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

                XmlDocument doc = BuildXmlDocument(className, lineNum);
                return doc;
            }
            return null;
        }

        private XmlDocument BuildXmlDocument(string className, string lineNum)
        {
            XmlDocument xmlDoc = new();
            xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", "yes");
            XmlElement rootElement = xmlDoc.CreateElement("eminus");
            xmlDoc.AppendChild(rootElement);
            rootElement.SetAttribute("authKey", _config.password);

            XmlElement loadElement = xmlDoc.CreateElement("loadclass");
            loadElement.SetAttribute("mode", "dialog");
            rootElement.AppendChild(loadElement);

            XmlElement elemClassName = xmlDoc.CreateElement("classname");
            XmlElement elemLineNum = xmlDoc.CreateElement("linenumber");
            elemClassName.InnerText = className;
            elemLineNum.InnerText = lineNum;
            loadElement.AppendChild(elemClassName);
            loadElement.AppendChild(elemLineNum);
            return xmlDoc;
        }

        #endregion

        #region IContextMenuEntry Member

        public string GetMenuText(IList<int> logLines, ILogLineColumnizer columnizer, ILogExpertCallback callback)
        {
            //not used
            return string.Empty;
        }

        public string GetMenuText(int logLinesCount, ILogLineColumnizer columnizer, ILogLine logline)
        {
            if (logLinesCount == 1 && BuildParam(logline) != null)
            {
                return "Load class in Eclipse";
            }
            else
            {
                return $"{DISABLED}Load class in Eclipse";
            }
        }

        public void MenuSelected(IList<int> logLines, ILogLineColumnizer columnizer, ILogExpertCallback callback)
        {
            //Not used
        }

        public void MenuSelected(int logLinesCount, ILogLineColumnizer columnizer, ILogLine logline)
        {
            if (logLinesCount != 1)
            {
                return;
            }

            XmlDocument doc = BuildParam(logline);

            if (doc == null)
            {
                MessageBox.Show("Cannot parse Java stack trace line", "LogExpert");
            }
            else
            {
                try
                {
                    TcpClient client = new(_config.host, _config.port);
                    NetworkStream stream = client.GetStream();
                    StreamWriter writer = new(stream);
                    doc.Save(writer);
                    writer.Flush();
                    stream.Flush();
                    writer.Close();
                    stream.Close(500);
                    client.Close();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "LogExpert");
                }
            }
        }

        #endregion


        #region ILogExpertPluginConfigurator Member

        public void LoadConfig(string configDir)
        {
            string configPath = configDir + CFG_FILE_NAME;

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
                    MessageBox.Show(e.Message, "Deserialize");
                    _config = new EminusConfig();
                }
            }
        }


        public void SaveConfig(string configDir)
        {
            FileInfo fileInfo = new(configDir + Path.DirectorySeparatorChar + CFG_FILE_NAME);

            dlg?.ApplyChanges();

            _config = tmpConfig.Clone();

            using StreamWriter sw = new(fileInfo.Create());
            JsonSerializer serializer = new();
            serializer.Serialize(sw, _config);
        }

        public bool HasEmbeddedForm()
        {
            return true;
        }

        public void ShowConfigForm(object panel)
        {
            dlg = new EminusConfigDlg(tmpConfig)
            {
                Parent = (Panel)panel
            };
            dlg.Show();
        }

        /// <summary>
        /// Implemented only for demonstration purposes. This function is called when the config button
        /// is pressed (HasEmbeddedForm() must return false for this).
        /// </summary>
        /// <param name="owner"></param>
        public void ShowConfigDialog(object owner)
        {
            dlg = new EminusConfigDlg(tmpConfig)
            {
                TopLevel = true,
                Owner = (Form)owner
            };

            dlg.ShowDialog();
            dlg.ApplyChanges();
        }


        public void HideConfigForm()
        {
            if (dlg != null)
            {
                dlg.ApplyChanges();
                dlg.Hide();
                dlg.Dispose();
                dlg = null;
            }
        }

        public void StartConfig()
        {
            tmpConfig = _config.Clone();
        }

        #endregion
    }
}