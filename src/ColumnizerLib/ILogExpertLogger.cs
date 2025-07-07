using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LogExpert;

/// <summary>
/// Simple Logger interface to let plugins log into LogExpert's application log file.
/// </summary>
public interface ILogExpertLogger
{
    #region Public methods

    /// <summary>
    /// Logs a message on INFO level to LogExpert#s log file. The logfile is only active in debug builds.
    /// The logger in LogExpert will automatically add the class and the method name of the caller.
    /// </summary>
    /// <param name="msg">A message to be logged.</param>
    void Info(string msg);
    void Info (IFormatProvider formatProvider, string msg);

    /// <summary>
    /// Logs a message on DEBUG level to LogExpert#s log file. The logfile is only active in debug builds.
    /// The logger in LogExpert will automatically add the class and the method name of the caller.
    /// </summary>
    /// <param name="msg">A message to be logged.</param>
    void Debug(string msg);

    /// <summary>
    /// Logs a message on WARN level to LogExpert#s log file. The logfile is only active in debug builds.
    /// The logger in LogExpert will automatically add the class and the method name of the caller.
    /// </summary>
    /// <param name="msg">A message to be logged.</param>
    void LogWarn(string msg);

    /// <summary>
    /// Logs a message on ERROR level to LogExpert#s log file. The logfile is only active in debug builds.
    /// The logger in LogExpert will automatically add the class and the method name of the caller.
    /// </summary>
    /// <param name="msg">A message to be logged.</param>
    void LogError(string msg);

    #endregion
}