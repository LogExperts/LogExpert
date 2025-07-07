using System;
using System.Collections.Generic;
using System.Text;

namespace LogExpert;

/// <summary>
/// Implement this interface to execute a self defined action when LogExpert detects a 
/// keyword on incomig log file content.
/// These kind of plugins can be used in the "Highlight and Action Triggers" dialog.
/// </summary>
public interface IKeywordAction
{
    #region Public methods

    /// <summary>
    /// Is called when LogExpert detects a keyword match which is configured for this plugin.
    /// The keywords are configured in the Highlight dialog.
    /// You have to implement this method to execute your desired actions.
    /// </summary>
    /// <param name="keyword">The keyword which triggered the call.</param>
    /// <param name="param">The parameter configured for the plugin launch (in the Highlight dialog).</param>
    /// <param name="callback">A callback which can be used by the plugin.</param>
    /// <param name="columnizer">The current columnizer. Can be used to obtain timestamps 
    /// (if supported by Columnizer) or to split the log line into fields.</param>
    /// <remarks>
    /// This method is called in a background thread from the process' thread pool (using BeginInvoke()). 
    /// So you cannot rely on state information retrieved by the given callback. E.g. the line count
    /// may change during the execution of the method. The only exception from this rule is the current line number
    /// retrieved from the callback. This is of course the line number of the line that has triggered
    /// the keyword match.
    /// </remarks>
    void Execute(string keyword, string param, ILogExpertCallback callback, ILogLineColumnizer columnizer);

    /// <summary>
    /// Return the name of your plugin here. The returned name is used for displaying the plugin list 
    /// in the settings.
    /// </summary>
    /// <returns>The name of the plugin.</returns>
    string GetName();

    /// <summary>
    /// Return a description of your plugin here. E.g. a short explanation of parameters. The descriptions
    /// will be displayed in the plugin chooser dialog which is used by the Highlight settings.
    /// </summary>
    /// <returns>The description of the plugin.</returns>
    string GetDescription();

    #endregion
}