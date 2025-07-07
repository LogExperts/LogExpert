
namespace LogExpert;

/// <summary>
/// A Columnizer can implement this interface if it has to show an own settings dialog to the user.
/// The Config button in LogExpert's columnizer dialog is enabled if a Columnizer implements this interface.
/// If you don't need a config dialog you don't have to implement this interface.
/// </summary>
public interface IColumnizerConfigurator
{
    #region Public methods

    /// <summary>
    /// This function is called if the user presses the Config button on the Columnizer dialog.
    /// Its up to the Columnizer plugin to show an own configuration dialog and store all
    /// required settings.
    /// </summary>
    /// <param name="callback">Callback interface with functions which can be used by the columnizer</param>
    /// <param name="configDir">The complete path to the directory where LogExpert stores its settings. 
    /// You can use this directory, if you want to. Please don't use the file name "settings.dat", because this
    /// name is used by LogExpert.
    /// </param>
    /// <remarks>
    /// This is the place to show a configuration dialog to the user. You have to handle all dialog stuff by yourself.
    /// It's also your own job to store the configuration in a config file or on the registry.
    /// The callback is passed to this function just in case you need the file name of the current log file
    /// or the line count etc. You can also use it to store different settings for every log file.
    /// You can use the callback to distinguish between different files. Its passed to all important 
    /// functions in the Columnizer.
    /// </remarks>
    void Configure(ILogLineColumnizerCallback callback, string configDir);

    /// <summary>
    /// This function will be called right after LogExpert has loaded your Columnizer class. Use this
    /// to load the configuration which was saved in the Configure() function.
    /// You have to hold the loaded config data in your Columnizer object.
    /// </summary>
    /// <param name="configDir">The complete path to the directory where LogExpert stores its settings. 
    /// You can use this directory, if you want to. Please don't use the file name "settings.dat", because this
    /// name is used by LogExpert.
    /// </param>
    void LoadConfig(string configDir);

    #endregion
}