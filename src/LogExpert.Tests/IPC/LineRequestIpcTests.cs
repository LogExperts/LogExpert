using LogExpert.Classes;
using LogExpert.Core.Classes.IPC;
using LogExpert.Core.Interfaces;
using LogExpert.UI.Extensions.LogWindow;

using Moq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NUnit.Framework;

namespace LogExpert.Tests.IPC;

[TestFixture]
internal sealed class LineRequestIpcTests
{
    [TestCase(false, IpcMessageType.NewWindow)]
    [TestCase(true, IpcMessageType.NewWindowOrLockedWindow)]
    public void SerializeAndDispatch_PreservesTargetAndRouting (bool singleInstance, IpcMessageType expectedType)
    {
        var files = new[] { @"C:\logs\application.log" };
        var json = Program.SerializeCommandIntoNonFormattedJSON(files, singleInstance, 1234);
        var message = JsonConvert.DeserializeObject<IpcMessage>(json)!;
        var proxy = new Mock<ILogExpertProxy>();

        Program.SendMessageToProxy(message, proxy.Object);

        Assert.That(message.Type, Is.EqualTo(expectedType));
        if (singleInstance)
        {
            proxy.Verify(p => p.NewWindowOrLockedWindow(files, 1234), Times.Once);
        }
        else
        {
            proxy.Verify(p => p.NewWindow(files, 1234), Times.Once);
        }

        proxy.VerifyNoOtherCalls();
    }
    [TestCase("{\"Files\":[\"application.log\"],\"TargetLine\":0}")]
    [TestCase("{\"Files\":[\"application.log\"],\"TargetLine\":-1}")]
    [TestCase("{\"Files\":[\"application.log\"],\"TargetLine\":2147483648}")]
    [TestCase("{\"Files\":[\"application.log\"],\"TargetLine\":1.5}")]
    [TestCase("{\"Files\":[\"application.log\"],\"TargetLine\":\"2\"}")]
    [TestCase("{\"Files\":[],\"TargetLine\":2}")]
    [TestCase("{\"Files\":null,\"TargetLine\":2}")]
    [TestCase("{\"Files\":[\"a.log\",\"b.log\"],\"TargetLine\":2}")]
    [TestCase("{\"Files\":[\"session.LXJ\"],\"TargetLine\":2}")]
    [TestCase("{\"Files\":[\"session.LXP\"],\"TargetLine\":2}")]
    public void Dispatch_InvalidIncomingTarget_DoesNotOpenFiles (string payload)
    {
        var proxy = new Mock<ILogExpertProxy>();
        var message = new IpcMessage { Payload = JObject.Parse(payload) };

        Assert.DoesNotThrow(() => Program.SendMessageToProxy(message, proxy.Object));

        proxy.VerifyNoOtherCalls();
    }

    [Test]
    public void Dispatch_LegacyPayload_LoadsWithoutTarget ()
    {
        var proxy = new Mock<ILogExpertProxy>();
        var message = JsonConvert.DeserializeObject<IpcMessage>("{\"Type\":0,\"Payload\":{\"Files\":[\"application.log\"]}}")!;

        Program.SendMessageToProxy(message, proxy.Object);

        proxy.Verify(p => p.LoadFiles(new[] { "application.log" }, null), Times.Once);
        proxy.VerifyNoOtherCalls();
    }

    [TestCase(IpcMessageType.Load, false)]
    [TestCase(IpcMessageType.NewWindowOrLockedWindow, false)]
    [TestCase(IpcMessageType.NewWindowOrLockedWindow, true)]
    public void Dispatch_LoadToExistingWindow_PreservesTarget (IpcMessageType type, bool locked)
    {
        var first = new Mock<ILogTabWindow>();
        var active = new Mock<ILogTabWindow>();
        var proxy = new LogExpertProxy(first.Object);
        proxy.NotifyWindowActivated(active.Object);
        AbstractLogTabWindow.StaticData.CurrentLockedMainWindow = locked ? first.Object : null;
        try
        {
            var message = JsonConvert.DeserializeObject<IpcMessage>(
                Program.SerializeCommandIntoNonFormattedJSON(["application.log"], true, 42))!;
            message.Type = type;

            Program.SendMessageToProxy(message, proxy);

            var destination = locked ? first : active;
            destination.Verify(w => w.LoadFiles(new[] { "application.log" }, 42), Times.Once);
            var other = locked ? active : first;
            other.Verify(w => w.LoadFiles(It.IsAny<string[]>(), It.IsAny<int?>()), Times.Never);
        }
        finally
        {
            AbstractLogTabWindow.StaticData.CurrentLockedMainWindow = null;
        }
    }

    [Test]
    public void NewWindow_MarshalsFileAndTargetTogetherToUiThread ()
    {
        var window = new Mock<ILogTabWindow>();
        var proxy = new LogExpertProxy(window.Object);
        object[]? forwardedArguments = null;
        _ = window.Setup(w => w.Invoke(It.IsAny<Delegate>(), It.IsAny<object[]>()))
            .Callback((Delegate _, object[] args) => forwardedArguments = args);

        proxy.NewWindow(["application.log"], 42);

        Assert.That(forwardedArguments, Has.Length.EqualTo(2));
        Assert.That(forwardedArguments![0], Is.EqualTo(["application.log"]));
        Assert.That(forwardedArguments[1], Is.EqualTo(42));
    }
}
