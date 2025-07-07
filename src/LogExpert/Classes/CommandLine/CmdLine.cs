/*
 * Taken from https://cmdline.codeplex.com/
 *
 */

//TODO: Replace with https://github.com/commandlineparser/commandline
//TODO: or with this https://github.com/natemcmaster/CommandLineUtils
namespace LogExpert.Classes.CommandLine;

/// <summary>
/// Represents an string command line parameter.
/// </summary>
public class CmdLineString (string name, bool required, string helpMessage) : CmdLineParameter(name, required, helpMessage)
{

    #region Public methods

    public static implicit operator string (CmdLineString s)
    {
        ArgumentNullException.ThrowIfNull(s, nameof(s));
        return s.Value;
    }

    public override string ToString ()
    {
        return Value;
    }

    #endregion
}

/// <summary>
/// Provides a simple strongly typed interface to work with command line parameters.
/// </summary>
public class CmdLine
{
    #region Fields

    // A private dictonary containing the parameters.
    private readonly Dictionary<string, CmdLineParameter> parameters = [];

    #endregion

    #region cTor

    /// <summary>
    /// Creats a new empty command line object.
    /// </summary>
    public CmdLine()
    {
    }

    #endregion

    #region Properties

    /// <summary>
    /// Returns a command line parameter by the name.
    /// </summary>
    /// <param name="name">The name of the parameter (the word after the initial hyphen (-).</param>
    /// <returns>A reference to the named comman line object.</returns>
    public CmdLineParameter this[string name]
    {
        get
        {
            if (parameters.TryGetValue(name, out CmdLineParameter value) == false)
            {
                throw new CmdLineException(name, "Not a registered parameter.");
            }
            return value;
        }
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Registers a parameter to be used and adds it to the help screen.
    /// </summary>
    /// <param name="parameter">The parameter to add.</param>
    public void RegisterParameter(CmdLineParameter parameter)
    {
        if (parameters.ContainsKey(parameter.Name))
        {
            throw new CmdLineException(parameter.Name, "Parameter is already registered.");
        }
        parameters.Add(parameter.Name, parameter);
    }

    /// <summary>
    /// Registers parameters to be used and adds hem to the help screen.
    /// </summary>
    /// <param name="parameters">The parameter to add.</param>
    public void RegisterParameter(CmdLineParameter[] parameters)
    {
        foreach (CmdLineParameter p in parameters)
        {
            RegisterParameter(p);
        }
    }


    /// <summary>
    /// Parses the command line and sets the value of each registered parmaters.
    /// </summary>
    /// <param name="args">The arguments array sent to main()</param>
    /// <returns>Any reminding strings after arguments has been processed.</returns>
    public string[] Parse(string[] args)
    {
        var i = 0;

        List<string> new_args = [];

        while (i < args.Length)
        {
            if (args[i].Length > 1 && args[i][0] == '-')
            {
                // The current string is a parameter name
                var key = args[i][1..].ToLower();
                var argsValue = string.Empty;
                i++;
                if (i < args.Length)
                {
                    if (args[i].Length > 0 && args[i][0] == '-')
                    {
                        // The next string is a new parameter, do not nothing
                    }
                    else
                    {
                        // The next string is a value, read the value and move forward
                        argsValue = args[i];
                        i++;
                    }
                }
                if (parameters.TryGetValue(key, out CmdLineParameter cmdLineParameter) == false)
                {
                    throw new CmdLineException(key, "Parameter is not allowed.");
                }

                if (cmdLineParameter.Exists)
                {
                    throw new CmdLineException(key, "Parameter is specified more than once.");
                }

                cmdLineParameter.SetValue(argsValue);
            }
            else
            {
                new_args.Add(args[i]);
                i++;
            }
        }


        // Check that required parameters are present in the command line.
        foreach (var key in parameters.Keys)
        {
            if (parameters[key].Required && parameters[key].Exists == false)
            {
                throw new CmdLineException(key, "Required parameter is not found.");
            }
        }

        return new_args.ToArray();
    }

    /// <summary>
    /// Generates the help screen.
    /// </summary>
    public string HelpScreen()
    {
        var len = 0;
        foreach (var key in parameters.Keys)
        {
            len = Math.Max(len, key.Length);
        }

        var help = "\nParameters:\n\n";
        foreach (var key in parameters.Keys)
        {
            var s = "-" + parameters[key].Name;
            while (s.Length < len + 3)
            {
                s += " ";
            }
            s += parameters[key].Help + "\n";
            help += s;
        }
        return help;
    }

    #endregion
}