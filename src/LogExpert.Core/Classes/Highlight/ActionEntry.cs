namespace LogExpert.Core.Classes.Highlight;

[Serializable]
public class ActionEntry : ICloneable
{
    #region Fields

    public string ActionParam { get; set; }

    public string PluginName { get; set; }

    public object Clone()
    {
        var actionEntry = new ActionEntry
        {
            PluginName = PluginName,
            ActionParam = ActionParam
        };

        return actionEntry;
    }

    #endregion
}