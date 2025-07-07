using LogExpert.Core.Classes.Highlight;

using System.Runtime.Versioning;

namespace LogExpert.UI.Dialogs;

[SupportedOSPlatform("windows")]
internal partial class KeywordActionDlg : Form
{
    #region Fields

    private readonly IDictionary<string, IKeywordAction> _actionDict = new Dictionary<string, IKeywordAction>();

    private IList<IKeywordAction> _keywordActionList;

    #endregion

    #region cTor

    public KeywordActionDlg(ActionEntry entry, IList<IKeywordAction> actionList)
    {
        _keywordActionList = actionList;
        ActionEntry = entry;

        InitializeComponent();

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        actionComboBox.Items.Clear();

        foreach (IKeywordAction action in actionList)
        {
            actionComboBox.Items.Add(action.GetName());
            _actionDict[action.GetName()] = action;
        }

        if (actionComboBox.Items.Count > 0)
        {
            if (ActionEntry.PluginName != null && _actionDict.ContainsKey(ActionEntry.PluginName))
            {
                actionComboBox.SelectedItem = ActionEntry.PluginName;
            }
            else
            {
                actionComboBox.SelectedIndex = 0;
            }
        }

        parameterTextBox.Text = ActionEntry.ActionParam;
    }

    #endregion

    #region Properties

    public ActionEntry ActionEntry { get; private set; }

    #endregion

    #region Events handler

    private void OnOkButtonClick(object sender, EventArgs e)
    {
        ActionEntry = new ActionEntry
        {
            ActionParam = parameterTextBox.Text
        };

        if (_actionDict.ContainsKey((string)actionComboBox.SelectedItem))
        {
            ActionEntry.PluginName = (string)actionComboBox.SelectedItem;
        }
    }

    private void OnActionComboBoxSelectedIndexChanged(object sender, EventArgs e)
    {
        commentTextBox.Text = _actionDict[(string)actionComboBox.SelectedItem].GetDescription();
    }

    #endregion
}