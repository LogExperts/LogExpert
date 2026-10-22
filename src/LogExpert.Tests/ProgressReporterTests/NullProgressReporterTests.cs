using LogExpert.Core.Classes.Log.ProgressReporters;

using NUnit.Framework;

namespace LogExpert.Tests.ProgressReporterTests;

/// <summary>
/// Unit tests for <see cref="NullProgressReporter"/>.
/// Verifies no-op behavior and that it is safe to use in benchmarks/tests.
/// </summary>
[TestFixture]
internal sealed class NullProgressReporterTests
{
    [Test]
    public void Instance_IsSingleton ()
    {
        var a = NullProgressReporter.Instance;
        var b = NullProgressReporter.Instance;
        Assert.That(a, Is.SameAs(b));
    }

    [Test]
    public void AllMethods_DoNotThrow ()
    {
        var reporter = NullProgressReporter.Instance;

        Assert.DoesNotThrow(() =>
        {
            reporter.ReportProgress("file.log", 100, 1000);
            reporter.ReportComplete("file.log", 1000, 1000);
            reporter.ReportNewFile("new.log", 0, 5000);
            reporter.ReportLoadingStarted("file.log");
            reporter.ReportLoadingFinished();
        });
    }

    [Test]
    public void Dispose_DoesNotThrow ()
    {
        Assert.DoesNotThrow(NullProgressReporter.Instance.Dispose);
    }

    [Test]
    public void Dispose_CanBeCalledMultipleTimes ()
    {
        var reporter = NullProgressReporter.Instance;
        Assert.DoesNotThrow(() =>
        {
            reporter.Dispose();
            reporter.Dispose();
        });
    }
}