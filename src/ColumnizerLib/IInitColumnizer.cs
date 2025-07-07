using System;
using System.Collections.Generic;
using System.Text;

namespace LogExpert;

/// <summary>
/// Implement this interface in your columnizer if you need to do some initialization work 
/// every time the columnizer is selected.
/// </summary>
/// <remarks>
/// <para>
/// The methods in this interface will be called in the GUI thread. So make sure that there's no
/// heavyweight work to do in your implementations.</para>
/// <para>
/// If a file is reloaded, the current Columnizer is set again. That means that the methods of this
/// interface will be called again. Generally you should do no assumptions about how often the 
/// methods will be called. The file is already loaded when the columnizer is set. So
/// you can use the methods in the given callbacks to get informations about the file or to
/// retrieve specific lines.
/// </para>
/// </remarks>
public interface IInitColumnizer
{
    #region Public methods

    /// <summary>
    /// This method is called when the Columnizer is selected as the current columnizer.
    /// </summary>
    /// <param name="callback">Callback that can be used to retrieve some informations, if needed.</param>
    void Selected(ILogLineColumnizerCallback callback);

    /// <summary>
    /// This method is called when the Columnizer is de-selected (i.e. when another Columnizer is
    /// selected).
    /// </summary>
    /// <param name="callback">Callback that can be used to retrieve some informations, if needed.</param>
    void DeSelected(ILogLineColumnizerCallback callback);

    #endregion
}