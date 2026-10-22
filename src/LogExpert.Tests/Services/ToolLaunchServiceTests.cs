using System.Runtime.Versioning;

using LogExpert.Core.Interfaces;
using LogExpert.UI.Services.ToolLaunchService;

using Moq;

using NUnit.Framework;

namespace LogExpert.Tests.Services;

[TestFixture]
[Apartment(ApartmentState.STA)]
[SupportedOSPlatform("windows")]
internal class ToolLaunchServiceTests
{
    private Mock<IPluginRegistry> _pluginRegistryMock = null!;
    private ToolLaunchService _sut = null!;

    [SetUp]
    public void SetUp ()
    {
        _pluginRegistryMock = new Mock<IPluginRegistry>();
        _ = _pluginRegistryMock.Setup(pr => pr.RegisteredColumnizers).Returns([]);

        _sut = new ToolLaunchService(_pluginRegistryMock.Object);
    }

    [Test]
    public void Launch_WithEmptyCmd_ReturnsHasErrorTrue ()
    {
        var request = new ToolLaunchRequest { Cmd = string.Empty, Args = string.Empty, SysoutPipe = false };

        var result = _sut.Launch(request);

        Assert.That(result.HasError, Is.True);
        Assert.That(result.ErrorMessage, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void Launch_WithValidCmdAndNoSysoutPipe_ReturnsSuccessWithNullPipeFileName ()
    {
        var request = new ToolLaunchRequest { Cmd = "cmd.exe", Args = "/c exit 0", SysoutPipe = false };

        var result = _sut.Launch(request);

        Assert.That(result.HasError, Is.False);
        Assert.That(result.PipeFileName, Is.Null);
    }

    [Test]
    public void Launch_WithValidCmdAndSysoutPipe_ReturnsPipeFileNamePointingToExistingFile ()
    {
        var request = new ToolLaunchRequest { Cmd = "cmd.exe", Args = "/c echo hello", SysoutPipe = true };

        var result = _sut.Launch(request);

        Assert.That(result.HasError, Is.False);
        Assert.That(result.PipeFileName, Is.Not.Null.And.Not.Empty);
        Assert.That(File.Exists(result.PipeFileName), Is.True);
    }
}
