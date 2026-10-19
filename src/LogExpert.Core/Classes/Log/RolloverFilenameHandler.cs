using ColumnizerLib;

using LogExpert.Core.Entities;
using LogExpert.Core.Interfaces;

using System.Collections.Generic;

namespace LogExpert.Core.Classes.Log;

/// <summary>
/// Handles rollover naming. The names built by the RolloverFilenameBuilder will be used
/// to check if the file exist. Names will be built by incrmenting an index and decrementing a date.
/// A configurable number of days in the past will be checked (date gaps may occur on days without log file activity).
/// Date checking is only performed if the format pattern contains a date format. Index checking
/// is only performed of the format pattern contains an index placeholder.
/// </summary>
public class RolloverFilenameHandler
{
    #region Fields

    private readonly RolloverFilenameBuilder _filenameBuilder;
    private readonly ILogFileInfo _logFileInfo;
    private readonly MultiFileOptions _options;
    private readonly TimeProvider _timeProvider;

    #endregion

    #region cTor

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="logFileInfo">The complete path of the logfile</param>
    /// <param name="options">Multifile option (e.g. format pattern)</param>
    /// <param name="timeProvider">Time source used to calculate the file-age cutoff.</param>
    public RolloverFilenameHandler(ILogFileInfo logFileInfo, MultiFileOptions options, TimeProvider timeProvider = null)
    {
        _options = options;
        _logFileInfo = logFileInfo;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _filenameBuilder = new RolloverFilenameBuilder(_options.FormatPattern);
        _filenameBuilder.SetFileName(logFileInfo.FileName);
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Returns a list of the built file names (complete path) which also exists on disk.
    /// The list is created by using the RolloverFilenameBuilder and checking for file existence.
    /// The first entry in the list contains the oldest file. The last entry contains the file given
    /// in the contructor.
    /// </summary>
    /// <returns></returns>
    public LinkedList<string> GetNameList(IPluginRegistry pluginRegistry)
    {
        LinkedList<string> fileList = new();
        
        var fileName = _filenameBuilder.BuildFileName();
        var filePath = _logFileInfo.DirectoryName + _logFileInfo.DirectorySeparatorChar + fileName;
        _ = fileList.AddFirst(filePath);
        
        var found = true;
        
        while (found)
        {
            found = false;
            // increment index and check if file exists
            if (_filenameBuilder.IsIndexPattern)
            {
                _filenameBuilder.Index += 1;
                fileName = _filenameBuilder.BuildFileName();
                filePath = _logFileInfo.DirectoryName + _logFileInfo.DirectorySeparatorChar + fileName;//TODO: Change to Directory.Combine
                var result = GetCandidateResult(filePath, pluginRegistry);
                if (result == CandidateResult.Include)
                {
                    _ = fileList.AddFirst(filePath);
                    found = true;
                    continue;
                }

                if (result == CandidateResult.TooOld)
                {
                    return fileList;
                }
            }

            // if file with index isn't found or no index is in format pattern, decrement the current date
            if (_filenameBuilder.IsDatePattern)
            {
                var tryCounter = 0;
                _filenameBuilder.Index = 0;
                while (tryCounter < _options.MaxDayTry)
                {
                    _filenameBuilder.DecrementDate();
                    fileName = _filenameBuilder.BuildFileName();
                    filePath = _logFileInfo.DirectoryName + _logFileInfo.DirectorySeparatorChar + fileName;//TODO: Change to Directory.Combine
                    var result = GetCandidateResult(filePath, pluginRegistry);
                    if (result == CandidateResult.Include)
                    {
                        _ = fileList.AddFirst(filePath);
                        found = true;
                        break;
                    }

                    if (result == CandidateResult.TooOld)
                    {
                        return fileList;
                    }

                    tryCounter++;
                }
            }
        }

        return fileList;
    }

    #endregion

    #region Private Methods

    private CandidateResult GetCandidateResult(string filePath, IPluginRegistry pluginRegistry)
    {
        var fs = pluginRegistry.FindFileSystemForUri(filePath);

        if(fs == null)
        {
            return CandidateResult.Missing;
        }

        var info = fs.GetLogfileInfo(filePath);
        if (info is null || !info.FileExists)
        {
            return CandidateResult.Missing;
        }

        if (_options.MaxFileAgeDays <= 0 || info.LastWriteTimeUtc is not { } lastWriteTimeUtc)
        {
            return CandidateResult.Include;
        }

        var cutoffUtc = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-_options.MaxFileAgeDays);
        return lastWriteTimeUtc >= cutoffUtc ? CandidateResult.Include : CandidateResult.TooOld;
    }

    private enum CandidateResult
    {
        Missing,
        Include,
        TooOld
    }

    #endregion
}