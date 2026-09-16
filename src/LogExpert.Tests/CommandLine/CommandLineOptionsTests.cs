using LogExpert.Classes;

using NUnit.Framework;

namespace LogExpert.Tests.CommandLine;

[TestFixture]
internal sealed class CommandLineOptionsTests
{
    [TestCase("--line")]
    [TestCase("-n")]
    public void Parse_LineOption_PreservesFileAndOneBasedTarget (string option)
    {
        var options = new CommandLineOptions();

        var result = options.Parse(["application.log", option, "1234"]);

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.GetValue(options.FilesArgument), Is.EqualTo(["application.log"]));
        Assert.That(result.GetValue(options.LineOption), Is.EqualTo(1234));
    }

    [TestCase("application.log --line")]
    [TestCase("application.log --line nope")]
    [TestCase("application.log --line 0")]
    [TestCase("application.log --line -1")]
    [TestCase("application.log --line 2147483648")]
    [TestCase("application.log --line 2 --line 3")]
    [TestCase("application.log --line 2 -n 3")]
    [TestCase("--line 2")]
    [TestCase("first.log second.log --line 2")]
    [TestCase("session.LXJ --line 2")]
    [TestCase("application.LXP --line 2")]
    public void Parse_InvalidLineRequest_ReportsError (string command)
    {
        var result = new CommandLineOptions().Parse(command.Split(' '));

        Assert.That(result.Errors, Is.Not.Empty);
        Assert.That(result.Errors[0].Message, Is.Not.Empty);
    }
    [TestCase("1", 1)]
    [TestCase("2147483647", int.MaxValue)]
    public void Parse_LineNumberBoundaries_AreAccepted (string value, int expected)
    {
        var options = new CommandLineOptions();
        var result = options.Parse(["--line", value, @"C:\logs with spaces\application.log"]);

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.GetValue(options.LineOption), Is.EqualTo(expected));
    }

    [TestCase("--config")]
    [TestCase("-c")]
    [TestCase("-config")]
    public void Parse_ConfigAndLineOptions_CanBeCombined (string configOption)
    {
        var options = new CommandLineOptions();
        var result = options.Parse([configOption, "settings.json", "application.log", "-n", "2"]);

        Assert.That(result.Errors, Is.Empty);
        Assert.That((result.GetValue(options.ConfigOption) ?? result.GetValue(options.LegacyConfigOption))!.Name,
            Is.EqualTo("settings.json"));
        Assert.That(result.GetValue(options.LineOption), Is.EqualTo(2));
    }

    [TestCase("")]
    [TestCase("first.log second.log")]
    [TestCase("session.lxj")]
    [TestCase("application.lxp")]
    public void Parse_WithoutLineOption_RetainsExistingInputs (string command)
    {
        var options = new CommandLineOptions();
        var result = options.Parse(command.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.GetValue(options.LineOption), Is.Null);
    }
}
