using System.Globalization;

namespace LogExpert.UI.Controls.LogWindow;

/// <summary>The notice above the main grid: how many lines hide-line rules remove, and the "Show hidden lines" override.</summary>
internal sealed class HiddenLinesBar : Panel
{
    private const int BAR_HEIGHT = 24;

    // Not AutoSize: an auto-sized label keeps its text height and sits at the top instead of centering beside the check box.
    private readonly Label _label = new() { Name = "hiddenLinesLabel", AutoSize = false, Dock = DockStyle.Left, TextAlign = ContentAlignment.MiddleLeft };
    private readonly CheckBox _checkBox = new() { Name = "showHiddenLinesCheckBox", AutoSize = true, Dock = DockStyle.Left };

    public HiddenLinesBar ()
    {
        Name = "hiddenLinesBar";
        Dock = DockStyle.Top;
        Visible = false;
        Padding = new Padding(4, 0, 4, 0);
        _checkBox.Text = Resources.LogWindow_UI_CheckBox_ShowHiddenLines;
        _checkBox.CheckedChanged += (_, _) => ShowHiddenLinesChanged?.Invoke(this, EventArgs.Empty);
        Controls.Add(_checkBox);
        Controls.Add(_label);
    }

    public event EventHandler? ShowHiddenLinesChanged;

    public bool ShowHiddenLines => _checkBox.Checked;

    public int BarHeight => LogicalToDeviceUnits(BAR_HEIGHT);

    /// <summary>Shows the count and the override; the bar is visible while lines are hidden or the override is on. Returns whether its visibility changed.</summary>
    public bool SetState (int hiddenCount, bool showHiddenLines)
    {
        _label.Text = string.Format(CultureInfo.CurrentCulture, Resources.LogWindow_UI_Label_HiddenLines, hiddenCount);
        _label.Width = _label.PreferredWidth + LogicalToDeviceUnits(12);
        _checkBox.Checked = showHiddenLines;

        var visible = hiddenCount > 0 || showHiddenLines;
        if (Visible == visible)
        {
            return false;
        }

        Visible = visible;
        return true;
    }
}
