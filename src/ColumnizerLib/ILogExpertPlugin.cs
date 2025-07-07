using System;
using System.Collections.Generic;
using System.Text;

namespace LogExpert;

/// <summary>
/// Implement this interface to get notified of various global events in LogExpert.
/// The interface can be implemented by all currently known type of LogExpert plugins (Columnizers,
/// keyword plugins, context menu plugins).
/// </summary>
public interface ILogExpertPlugin
{
    #region Public methods

    /// <summary>
    /// Called on application exit. May be used for cleanup purposes,
    /// </summary>
    void AppExiting();

    /// <summary>
    /// Called when the plugin is loaded at plugin registration while LogExpert startup.
    /// </summary>
    void PluginLoaded();

    #endregion
}