using System.CommandLine;
using System.Globalization;

using LogExpert.Core.Classes.IPC;

namespace LogExpert.Classes;

internal sealed class CommandLineOptions
{
    public Option<FileInfo?> ConfigOption { get; } = new("--config", "-c")
    {
        Description = "A configuration (settings) file"
    };

    public Option<FileInfo?> LegacyConfigOption { get; } = new("-config")
    {
        Hidden = true
    };

    public Option<int?> LineOption { get; } = new("--line", "-n")
    {
        Description = "Open one log file at this one-based line number",
        Arity = ArgumentArity.ExactlyOne
    };

    public Argument<string[]> FilesArgument { get; } = new("files")
    {
        Description = "Log files (.log etc.) or session files (.lxj) to open"
    };

    public CommandLineOptions ()
    {
        LineOption.Validators.Add(result =>
        {
            if (result.IdentifierTokenCount > 1)
            {
                result.AddError("Specify --line / -n only once.");
            }
        });
    }

    public ParseResult Parse (string[] args)
    {
        RootCommand command = new("LogExpert — log file viewer.")
        {
            ConfigOption,
            LegacyConfigOption,
            LineOption,
            FilesArgument
        };

        command.Validators.Add(result =>
        {
            // Command validators run before option conversion errors are reported.
            if (result.GetResult(LineOption) is not { } lineResult
                || lineResult.Tokens.Count != 1
                || !int.TryParse(lineResult.Tokens[0].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var targetLine))
            {
                return;
            }

            var request = new LoadPayload
            {
                Files = [.. result.GetValue(FilesArgument) ?? []],
                TargetLine = targetLine
            };
            if (request.GetValidationError() is string error)
            {
                result.AddError(error);
            }
        });

        return command.Parse(args);
    }
}
