using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LogExpert.Core.Config;

[Serializable]
[JsonConverter(typeof(StringEnumConverter))]
public enum SessionSaveLocation
{
    //Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + Path.DirectorySeparatorChar + "LogExpert"
    /// <summary>
    /// <see cref="Environment.SpecialFolder.MyDocuments"/>
    /// </summary>
    DocumentsDir = 0,
    //same directory as the logfile
    SameDir = 1,
    //uses configured folder to save the session files
    /// <summary>
    /// <see cref="Preferences.SessionSaveDirectory"/>
    /// </summary>
    OwnDir = 2,
    /// <summary>
    /// <see cref="Windows.Forms.Application.StartupPath"/>
    /// </summary>
    ApplicationStartupDir = 3,
    LoadedSessionFile = 4
}