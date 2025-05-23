/*
 * Taken from https://cmdline.codeplex.com/
 *
 */

//TODO Replace with https://github.com/commandlineparser/commandline
namespace LogExpert.Core.Classes
{
    /// <summary>
    /// Represents an error occuring during command line parsing.
    /// </summary>
    public class CmdLineException : Exception
    {
        #region cTor

        public CmdLineException(string parameter, string message)
            :
            base(string.Format("Syntax error of parameter -{0}: {1}", parameter, message))
        {
        }

        public CmdLineException(string message)
            :
            base(message)
        {
        }

        #endregion
    }

    /// <summary>
    /// Represents a command line parameter.
    /// Parameters are words in the command line beginning with a hyphen (-).
    /// The value of the parameter is the next word in
    /// </summary>
    /// <remarks>
    /// Creates a new instance of this class.
    /// </remarks>
    /// <param name="name">Name of parameter.</param>
    /// <param name="required">Require that the parameter is present in the command line.</param>
    /// <param name="helpMessage">The explanation of the parameter to add to the help screen.</param>
    public class CmdLineParameter(string name, bool required, string helpMessage)
    {
        #region Properties

        /// <summary>
        /// Returns the value of the parameter.
        /// </summary>
        public string Value { get; private set; } = "";

        /// <summary>
        /// Returns the help message associated with the parameter.
        /// </summary>
        public string Help { get; } = helpMessage;

        /// <summary>
        /// Returns true if the parameter was found in the command line.
        /// </summary>
        public bool Exists { get; private set; } = false;

        /// <summary>
        /// Returns true if the parameter is required in the command line.
        /// </summary>
        public bool Required { get; } = required;

        /// <summary>
        /// Returns the name of the parameter.
        /// </summary>
        public string Name { get; } = name;

        #endregion

        #region Public methods

        /// <summary>
        /// Sets the value of the parameter.
        /// </summary>
        /// <param name="value">A string containing a integer expression.</param>
        public virtual void SetValue(string value)
        {
            Value = value;
            Exists = true;
        }

        #endregion
    }

    /// <summary>
    /// Represents an integer command line parameter.
    /// </summary>
    public class CmdLineInt : CmdLineParameter
    {
        #region Fields

        private readonly int _max = int.MaxValue;
        private readonly int _min = int.MinValue;

        #endregion

        #region cTor

        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        /// <param name="name">Name of parameter.</param>
        /// <param name="required">Require that the parameter is present in the command line.</param>
        /// <param name="helpMessage">The explanation of the parameter to add to the help screen.</param>
        public CmdLineInt(string name, bool required, string helpMessage)
            : base(name, required, helpMessage)
        {
        }

        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        /// <param name="name">Name of parameter.</param>
        /// <param name="required">Require that the parameter is present in the command line.</param>
        /// <param name="helpMessage">The explanation of the parameter to add to the help screen.</param>
        /// <param name="min">The minimum value of the parameter.</param>
        /// <param name="max">The maximum valie of the parameter.</param>
        public CmdLineInt(string name, bool required, string helpMessage, int min, int max)
            : base(name, required, helpMessage)
        {
            _min = min;
            _max = max;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Returns the current value of the parameter.
        /// </summary>
        new public int Value { get; private set; }

        #endregion

        #region Public methods

        /// <summary>
        /// Sets the value of the parameter.
        /// </summary>
        /// <param name="value">A string containing a integer expression.</param>
        public override void SetValue(string value)
        {
            base.SetValue(value);
            int i = 0;
            try
            {
                i = Convert.ToInt32(value);
            }
            catch (Exception)
            {
                throw new CmdLineException(Name, "Value is not an integer.");
            }
            if (i < _min)
            {
                throw new CmdLineException(Name, string.Format("Value must be greather or equal to {0}.", _min));
            }
            if (i > _max)
            {
                throw new CmdLineException(Name, string.Format("Value must be less or equal to {0}.", _max));
            }
            Value = i;
        }

        /// <summary>
        /// A implicit converion to a int data type.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static implicit operator int(CmdLineInt s)
        {
            return s.Value;
        }

        #endregion
    }

    /// <summary>
    /// Represents an string command line parameter.
    /// </summary>
    public class CmdLineString(string name, bool required, string helpMessage) : CmdLineParameter(name, required, helpMessage)
    {

        #region Public methods

        public static implicit operator string(CmdLineString s)
        {
            return s.Value;
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
                if (parameters.TryGetValue(name, out CmdLineParameter? value) == false)
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
            int i = 0;

            List<string> new_args = [];

            while (i < args.Length)
            {
                if (args[i].Length > 1 && args[i][0] == '-')
                {
                    // The current string is a parameter name
                    string key = args[i][1..].ToLower();
                    string argsValue = string.Empty;
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
                    if (parameters.TryGetValue(key, out CmdLineParameter? cmdLineParameter) == false)
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
            foreach (string key in parameters.Keys)
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
            int len = 0;
            foreach (string key in parameters.Keys)
            {
                len = Math.Max(len, key.Length);
            }

            string help = "\nParameters:\n\n";
            foreach (string key in parameters.Keys)
            {
                string s = "-" + parameters[key].Name;
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

            string error = string.Empty;

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
}