using System.Runtime.Versioning;

using LogExpert.Core.Interface;

namespace LogExpert.Dialogs;

[SupportedOSPlatform("windows")]
internal partial class FilterSelectorForm : Form //TODO: Can this be changed to UserControl?
{
    #region Fields

    private readonly ILogLineColumnizerCallback _callback;
    private readonly IList<ILogLineColumnizer> _columnizerList;

    #endregion

    #region cTor

    public FilterSelectorForm (IList<ILogLineColumnizer> existingColumnizerList, ILogLineColumnizer currentColumnizer, ILogLineColumnizerCallback callback, IConfigManager configManager)
    {
        SelectedColumnizer = currentColumnizer;
        _callback = callback;
        InitializeComponent();

        ConfigManager = configManager;

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        filterComboBox.SelectedIndexChanged += OnFilterComboBoxSelectedIndexChanged;

        // for the currently selected columnizer use the current instance and not the template instance from
        // columnizer registry. This ensures that changes made in columnizer config dialogs
        // will apply to the current instance
        _columnizerList = new List<ILogLineColumnizer>();

        foreach (ILogLineColumnizer col in existingColumnizerList)
        {
            _columnizerList.Add(col.GetType() == SelectedColumnizer.GetType() ? SelectedColumnizer : col);
        }

        foreach (ILogLineColumnizer col in _columnizerList)
        {
            filterComboBox.Items.Add(col);
        }

        foreach (ILogLineColumnizer columnizer in _columnizerList)
        {
            if (columnizer.GetType() == SelectedColumnizer.GetType())
            {
                filterComboBox.SelectedItem = columnizer;
                break;
            }
        }
    }

    #endregion

    #region Properties

    public ILogLineColumnizer SelectedColumnizer { get; private set; }

    public bool ApplyToAll => applyToAllCheckBox.Checked;

    public bool IsConfigPressed { get; private set; }
    public IConfigManager ConfigManager { get; }

    #endregion

    #region Events handler

    private void OnFilterComboBoxSelectedIndexChanged (object sender, EventArgs e)
    {
        ILogLineColumnizer col = _columnizerList[filterComboBox.SelectedIndex];
        SelectedColumnizer = col;
        var description = col.GetDescription();
        description += "\r\nSupports timeshift: " + (SelectedColumnizer.IsTimeshiftImplemented() ? "Yes" : "No");
        commentTextBox.Text = description;
        configButton.Enabled = SelectedColumnizer is IColumnizerConfigurator;
    }


    //TODO: Check if this logic can be remoed from this class and remove all the config manager instances from here.
    private void OnConfigButtonClick (object sender, EventArgs e)
    {
        if (SelectedColumnizer is IColumnizerConfigurator configurator)
        {
            var configDir = ConfigManager.ConfigDir;

            if (ConfigManager.Settings.Preferences.PortableMode)
            {
                configDir = ConfigManager.PortableModeDir;
            }

            configurator.Configure(_callback, configDir);
            IsConfigPressed = true;
        }
    }

    #endregion
}