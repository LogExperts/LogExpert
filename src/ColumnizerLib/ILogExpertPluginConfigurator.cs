using System;
using System.Collections.Generic;
using System.Text;

namespace LogExpert;

/// <summary>
/// If your context menu plugin or keyword action plugin has some configuration it should 
/// implement this interface.
/// When your plugin has implemented this interface, it will get notified if it has to
/// show a config dialog and to save/load config data.<br></br>
/// Like in the IColumnizerConfigurator, you have to do all the saving and loading stuff
/// by yourself.
/// </summary>
public interface ILogExpertPluginConfigurator
{
    #region Public methods

    /// <summary>
    /// Return whether your plugin will provide an embedded config dialog or wants to provide
    /// a 'popup' dialog for the configuration.<br></br><br></br>
    /// 'Embedded' means that the dialog is shown directly in the Settings dialog of LogExpert on the
    /// right pane of the plugin config tab.
    /// </summary>
    /// <returns>Return true if your plugin config dialog should be displayed embedded.</returns>
    bool HasEmbeddedForm();

    /// <summary>
    /// This function is called when LogExpert fills the list of plugins in the Settings dialog.
    /// This is the right time to create a 'temporary copy' of your current settings. The temporary copy
    /// can be used for initializing the config dialogs.
    /// </summary>
    void StartConfig();

    /// <summary>
    /// Implement this function if your plugins uses an embedded config dialog.
    /// This function is called when the user selects the plugin in the list of the Settings dialog
    /// and the plugin uses an embedded dialog.<br></br><br></br>
    /// You have to create a non-toplevel dialog and set the given parentPanel as the parent of your 
    /// dialog. Then make your dialog visible (using Show()).
    /// You don't need an OK or Cancel button. Changes made in the dialog should be retrieved
    /// to a temporary config every time the <see cref="HideConfigForm"/> function is called.
    /// The temporary config should be permanently stored when the <see cref="SaveConfig"/> function
    /// is called.
    /// </summary>
    /// <seealso cref="HasEmbeddedForm"/>
    /// <param name="parentPanel">Set this panel as the parent for you config dialog.</param>
    void ShowConfigForm(object parentPanel);

    /// <summary>
    /// Implement this function if your plugin uses an own top level dialog for the configuration (modal config dialog).
    /// This function is called if the user clicks on the 'Config' button on the plugin settings.
    /// <br></br><br></br>
    /// You have to create a top level dialog and set the given Form as the owner. Then show
    /// the dialog as a modal window (Form.ShowDialog()). Changes made in the dialog should be retrieved
    /// after Form.ShowDialog() returns and then put to the temporary copy of your config.
    /// The temporary copy config should be permanently stored when the <see cref="SaveConfig"/> function
    /// is called.
    /// </summary>
    /// <seealso cref="HasEmbeddedForm"/>
    /// <param name="owner">Set the given Form as the owner of your dialog.</param>
    void ShowConfigDialog(object owner);

    /// <summary>
    /// This function is called when the user selects another plugin in the list. You should retrieve
    /// the changes made in the config dialog to a temporary copy of the config and destroy your dialog.
    /// Don't make the changes permanent here, because the user may click the cancel button of LogExpert's 
    /// Settings dialog. In this case he/she would expect the changes to be discarded.<br></br>
    /// The right place for making changes permanent is the <see cref="SaveConfig"/> function.
    /// </summary>
    /// <remarks>
    /// The method is also called when the settings dialog is closed. If the settings dialog is closed
    /// by OK button this method is called before the <see cref="SaveConfig"/> method.
    /// </remarks>
    void HideConfigForm();

    /// <summary>
    /// Called by LogExpert if the user clicks the OK button in LogExpert's Settings dialog.
    /// Save your temporary copy of the config here.
    /// </summary>
    /// <param name="configDir">The location where LogExpert stores its settings.</param>
    void SaveConfig(string configDir);

    /// <summary>
    /// This function is called when LogExpert is started and scans the plugin directory.
    /// You should load your settings here.
    /// </summary>
    /// <param name="configDir">The location where LogExpert stores its settings.</param>
    void LoadConfig(string configDir);

    #endregion
}