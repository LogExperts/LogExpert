using Newtonsoft.Json;

namespace LogExpert.Core.Classes.Persister;

/// <summary>
/// Persisted workspace contents of a Session (.lxj): a list of log file paths
/// plus a tab/window layout XML blob. See CONTEXT.md "Sessions".
/// </summary>
[Serializable]
public class SessionData
{
    #region Fields

    /// <summary>
    /// Gets or sets the list of log file paths included in this Session.
    /// May contain references to Session File (.lxp) entries that resolve to logs.
    /// </summary>
    public List<string> FileNames { get; set; } = [];

    /// <summary>
    /// Gets or sets the XML representation of the tab layout configuration.
    /// </summary>
    public string TabLayoutXml { get; set; }

    /// <summary>
    /// Gets or sets the full file path to the Session (.lxj) on disk.
    /// </summary>
    /// <remarks>
    /// JSON property name pinned to "SessionFilePath" so existing .lxj files (which
    /// were written under the previous "Project" terminology) continue to deserialize
    /// without migration. The value is also re-set at load time from the file path,
    /// so even old files without this key load correctly.
    /// </remarks>
    [JsonProperty("SessionFilePath")]
    public string SessionFilePath { get; set; }

    #endregion
}
