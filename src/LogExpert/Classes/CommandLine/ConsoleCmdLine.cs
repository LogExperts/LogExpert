/*
 * Taken from https://cmdline.codeplex.com/
 *
 */

//TODO: Replace with https://github.com/commandlineparser/commandline
//TODO: or with this https://github.com/natemcmaster/CommandLineUtils
namespace LogExpert.Classes.CommandLine;

/// <summary>
/// Represents a CmdLine object to use with console applications.
/// The -help parameter will be registered automatically.
/// Any errors will be written to the console instead of generating exceptions.
/// </summary>
public class ConsoleCmdLine : CmdLine
{
    #region cTor

    public ConsoleCmdLine()
    {
        RegisterParameter(new CmdLineString("help", false, "Prints the help screen."));
    }

    #endregion

    #region Public methods

    public new string[] Parse(string[] args)
    {
        string[] ret = [];

        var error = string.Empty;

        try
        {
            ret = base.Parse(args);
        }
        catch (CmdLineException ex)
        {
            error = ex.Message;
        }

        if (this["help"].Exists)
        {
            Console.WriteLine(HelpScreen());
            Environment.Exit(0);
        }

        if (error != string.Empty)
        {
            Console.WriteLine(error);
            Console.WriteLine("Use -help for more information.");
            Environment.Exit(1);
        }

        return ret;
    }

    #endregion
}