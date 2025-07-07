using LogExpert.Core.Classes;

namespace LogExpert.Core.Config;

[Serializable]
public class ToolEntry
{
    public string Args { get; set; } = string.Empty;

    public string Cmd { get; set; } = string.Empty;

    public string ColumnizerName { get; set; } = string.Empty;

    public string IconFile { get; set; }

    public int IconIndex { get; set; }

    public bool IsFavourite { get; set; }

    public string Name { get; set; }

    public bool Sysout { get; set; }

    public string WorkingDir { get; set; } = string.Empty;

    #region Public methods

    public override string ToString ()
    {
        return Util.IsNull(Name) ? Cmd : Name;
    }

    public ToolEntry Clone ()
    {
        ToolEntry clone = new()
        {
            Cmd = Cmd,
            Args = Args,
            Name = Name,
            Sysout = Sysout,
            ColumnizerName = ColumnizerName,
            IsFavourite = IsFavourite,
            IconFile = IconFile,
            IconIndex = IconIndex,
            WorkingDir = WorkingDir
        };
        return clone;
    }

    #endregion
}