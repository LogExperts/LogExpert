using System;

namespace LogExpert.Core.Config;

[Serializable]
public enum SessionSaveLocation
{
    //Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + Path.DirectorySeparatorChar + "LogExpert"
    /// <summary>
    /// <see cref="Environment.SpecialFolder.MyDocuments"/>
    /// </summary>
    DocumentsDir,
    //same directory as the logfile
    SameDir,
    //uses configured folder to save the session files 
    /// <summary>
    /// <see cref="Preferences.SessionSaveDirectory"/>
    /// </summary>
    OwnDir,
    /// <summary>
    /// <see cref="Windows.Forms.Application.StartupPath"/>
    /// </summary>
    ApplicationStartupDir,
    LoadedSessionFile
}