/*
 * Taken from https://cmdline.codeplex.com/
 *
 */

//TODO: Replace with https://github.com/commandlineparser/commandline
//TODO: or with this https://github.com/natemcmaster/CommandLineUtils
namespace LogExpert.Classes.CommandLine;

/// <summary>
/// Represents an integer command line parameter.
/// </summary>
public class CmdLineInt : CmdLineParameter
{
    #region Fields

    private readonly int _max;
    private readonly int _min;

    #endregion

    #region cTor

    /// <summary>
    /// Creates a new instance of this class.
    /// </summary>
    /// <param name="name">Name of parameter.</param>
    /// <param name="required">Require that the parameter is present in the command line.</param>
    /// <param name="helpMessage">The explanation of the parameter to add to the help screen.</param>
    public CmdLineInt (string name, bool required, string helpMessage)
        : base(name, required, helpMessage)
    {
        _max = int.MaxValue;
        _min = int.MinValue;
    }

    /// <summary>
    /// Creates a new instance of this class.
    /// </summary>
    /// <param name="name">Name of parameter.</param>
    /// <param name="required">Require that the parameter is present in the command line.</param>
    /// <param name="helpMessage">The explanation of the parameter to add to the help screen.</param>
    /// <param name="min">The minimum value of the parameter.</param>
    /// <param name="max">The maximum valie of the parameter.</param>
    public CmdLineInt (string name, bool required, string helpMessage, int min, int max)
        : base(name, required, helpMessage)
    {
        _max = min;
        _max = max;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Returns the current value of the parameter.
    /// </summary>
    public new int Value { get; private set; }

    #endregion

    #region Public methods

    /// <summary>
    /// Sets the value of the parameter.
    /// </summary>
    /// <param name="value">A string containing a integer expression.</param>
    public override void SetValue (string value)
    {
        base.SetValue(value);
        int i;
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
            throw new CmdLineException(Name, $"Value must be greather or equal to {_min}.");
        }

        if (i > _max)
        {
            throw new CmdLineException(Name, $"Value must be less or equal to {_max}.");
        }

        Value = i;
    }

    /// <summary>
    /// A implicit converion to a int data type.
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static implicit operator int (CmdLineInt s) => s.Value;

    #endregion
}