namespace LogExpert.Dialogs;

internal partial class SettingsDialog
{
    private readonly TabPage _tabPageMarkerBar = new();
    private readonly FlowLayoutPanel _markerBarOptions = new();
    private readonly CheckBox _checkBoxShowMarkerBar = new();
    private readonly CheckBox _checkBoxShowHighlightMarkers = new();
    private readonly CheckBox _checkBoxShowBookmarkMarkers = new();
    private readonly CheckBox _checkBoxShowSearchMarkers = new();
    private readonly CheckBox _checkBoxShowFilterMarkers = new();

    private void InitializeMarkerBar ()
    {
        _tabPageMarkerBar.Name = "tabPageMarkerBar";
        _tabPageMarkerBar.Text = Resources.SettingsDialog_UI_TabPage_tabPageMarkerBar;
        _tabPageMarkerBar.Padding = new Padding(8);
        _tabPageMarkerBar.UseVisualStyleBackColor = true;

        _markerBarOptions.Dock = DockStyle.Fill;
        _markerBarOptions.FlowDirection = FlowDirection.TopDown;
        _markerBarOptions.WrapContents = false;
        _markerBarOptions.AutoScroll = true;

        _checkBoxShowMarkerBar.Name = "checkBoxShowMarkerBar";
        _checkBoxShowHighlightMarkers.Name = "checkBoxShowHighlightMarkers";
        _checkBoxShowBookmarkMarkers.Name = "checkBoxShowBookmarkMarkers";
        _checkBoxShowSearchMarkers.Name = "checkBoxShowSearchMarkers";
        _checkBoxShowFilterMarkers.Name = "checkBoxShowFilterMarkers";

        foreach (var checkBox in new[]
        {
            _checkBoxShowMarkerBar,
            _checkBoxShowHighlightMarkers,
            _checkBoxShowBookmarkMarkers,
            _checkBoxShowSearchMarkers,
            _checkBoxShowFilterMarkers
        })
        {
            checkBox.AutoSize = true;
            checkBox.Margin = new Padding(3);
        }

        _markerBarOptions.Controls.Add(_checkBoxShowMarkerBar);
        _markerBarOptions.Controls.Add(_checkBoxShowHighlightMarkers);
        _markerBarOptions.Controls.Add(_checkBoxShowBookmarkMarkers);
        _markerBarOptions.Controls.Add(_checkBoxShowSearchMarkers);
        _markerBarOptions.Controls.Add(_checkBoxShowFilterMarkers);
        _tabPageMarkerBar.Controls.Add(_markerBarOptions);
        tabControlSettings.TabPages.Add(_tabPageMarkerBar);
    }

    private void FillMarkerBarTab ()
    {
        _checkBoxShowMarkerBar.Checked = Preferences.ShowMarkerBar;
        _checkBoxShowHighlightMarkers.Checked = Preferences.ShowHighlightMarkers;
        _checkBoxShowBookmarkMarkers.Checked = Preferences.ShowBookmarkMarkers;
        _checkBoxShowSearchMarkers.Checked = Preferences.ShowSearchMarkers;
        _checkBoxShowFilterMarkers.Checked = Preferences.ShowFilterMarkers;
    }

    internal void SaveMarkerBarTab ()
    {
        Preferences.ShowMarkerBar = _checkBoxShowMarkerBar.Checked;
        Preferences.ShowHighlightMarkers = _checkBoxShowHighlightMarkers.Checked;
        Preferences.ShowBookmarkMarkers = _checkBoxShowBookmarkMarkers.Checked;
        Preferences.ShowSearchMarkers = _checkBoxShowSearchMarkers.Checked;
        Preferences.ShowFilterMarkers = _checkBoxShowFilterMarkers.Checked;
    }
}