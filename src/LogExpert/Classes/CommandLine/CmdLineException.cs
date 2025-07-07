/*
 * Taken from https://cmdline.codeplex.com/
 *
 */

//TODO: Replace with https://github.com/commandlineparser/commandline
//TODO: or with this https://github.com/natemcmaster/CommandLineUtils
namespace LogExpert.Classes.CommandLine;

/// <summary>
/// Represents an error occuring during command line parsing.
/// </summary>
public class CmdLineException : Exception
{
    #region cTor

    public CmdLineException (string parameter, string message) : base($"Syntax error of parameter -{parameter}: {message}")
    {
    }

    public CmdLineException (string message) : base(message)
    {
    }

    public CmdLineException ()
    {
    }

    public CmdLineException (string message, Exception innerException) : base(message, innerException)
    {
    }

    #endregion
}