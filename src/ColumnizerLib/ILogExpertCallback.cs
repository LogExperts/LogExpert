using System.Collections.Generic;

namespace LogExpert
{
    /// <summary>
    /// This callback interface is implemented by LogExpert. You can use it e.g. when implementing a
    /// context menu plugin.
    /// </summary>
    public interface ILogExpertCallback : ILogLineColumnizerCallback
    {
        #region Public methods

        /// <summary>
        /// Call this function to add a new temporary file tab to LogExpert. This may be usefull
        /// if your plugin creates some output into a file which has to be shown in LogExpert.
        /// </summary>
        /// <param name="fileName">Path of the file to be loaded.</param>
        /// <param name="title">Title shown on the tab.</param>
        /// <remarks>
        /// The file tab is internally handled like the temp file tabs which LogExpert uses for
        /// FilterTabs or clipboard copy tabs.
        /// This has some implications:
        /// <ul>
        ///   <li>The file path is not shown. Only the title is shown.</li>
        ///   <li>The encoding of the file is expected to be 2-byte Unicode!</li>
        ///   <li>The file will not be added to the history of opened files.</li>
        ///   <li>The file will be deleted when closing the tab!</li>
        /// </ul>
        /// </remarks>
        void AddTempFileTab(string fileName, string title);

        /// <summary>
        /// With this function you can create a new tab and add a bunch of text lines to it.
        /// </summary>
        /// <param name="lineEntryList">A list with LineEntry items containing text and an
        ///     optional reference to the original file location.</param>
        /// <param name="title">The title for the new tab.</param>
        /// <remarks>
        /// <para>
        /// The lines are given by a list of <see cref="LineEntry"/>. If you set the lineNumber field
        /// in each LineEntry to a lineNumber of the original logfile (the logfile for which the context
        /// menu is called for), you can create a 'link' from the line of your 'target output' to a line
        /// in the 'source tab'.
        /// </para>
        /// <para>
        /// The user can then navigate from the line in the new tab to the referenced
        /// line in the original file (by using "locate in original file" from the context menu).
        /// This is especially useful for plugins that generate output lines which are directly associated
        /// to the selected input lines.
        /// </para>
        /// <para>
        /// If you can't provide a reference to a location in the logfile, set the line number to -1. This
        /// will disable the "locate in original file" menu entry.
        /// </para>
        /// </remarks>
        void AddPipedTab(IList<LineEntry> lineEntryList, string title);

        /// <summary>
        /// Returns the title of the current tab (the tab for which the context menu plugin was called for).
        /// </summary>
        /// <returns></returns>
        string GetTabTitle();

        #endregion
    }
}