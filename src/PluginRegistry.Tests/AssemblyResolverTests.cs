using System.Reflection;
using System.Reflection.Emit;

using NUnit.Framework;

namespace LogExpert.PluginRegistry.Tests;

/// <summary>
/// The assembly resolver is reachable from type names stored in settings and .lxp files,
/// so it must never load assemblies from outside the application and plugin directories.
/// </summary>
[TestFixture]
[Category("PathTraversal")]
[Category("Security")]
public class AssemblyResolverTests
{
    private string _probeDirectory = null!;
    private string _probeAssemblyName = null!;

    [SetUp]
    public void SetUp ()
    {
        var id = Guid.NewGuid().ToString("N");
        _probeAssemblyName = "ResolveProbe_" + id;

        // Sibling of the base directory, so it is reachable through "../" but outside it.
        _probeDirectory = Path.GetFullPath(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "..", "resolve-probe-" + id));
        _ = Directory.CreateDirectory(_probeDirectory);

        var assemblyBuilder = new PersistedAssemblyBuilder(new AssemblyName(_probeAssemblyName), typeof(object).Assembly);
        var typeBuilder = assemblyBuilder.DefineDynamicModule(_probeAssemblyName).DefineType("Probe", TypeAttributes.Public);
        _ = typeBuilder.CreateType();
        assemblyBuilder.Save(Path.Join(_probeDirectory, _probeAssemblyName + ".dll"));
    }

    [TearDown]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Unit Tests")]
    public void TearDown ()
    {
        try
        {
            Directory.Delete(_probeDirectory, recursive: true);
        }
        catch
        {
            // The DLL stays locked if a regression loaded it; the assertion already reported that.
        }
    }

    [Test]
    public void ResolveHandler_AssemblyNameWithRelativePath_DoesNotLoadAssemblyOutsideBaseDirectory ()
    {
        var traversalName = $"../{Path.GetFileName(_probeDirectory)}/{_probeAssemblyName}";

        // Call the handler directly: the test host installs its own resolvers that would mask the result.
        var handler = typeof(PluginRegistry).GetMethod("ColumnizerResolveEventHandler", BindingFlags.NonPublic | BindingFlags.Static);
        var resolved = handler!.Invoke(null, [null, new ResolveEventArgs(traversalName)]);

        Assert.That(resolved, Is.Null);
    }

    [Test]
    public void ResolveHandler_PlainAssemblyName_IsStillResolvedFromBaseDirectory ()
    {
        var handler = typeof(PluginRegistry).GetMethod("ColumnizerResolveEventHandler", BindingFlags.NonPublic | BindingFlags.Static);
        var resolved = handler!.Invoke(null, [null, new ResolveEventArgs("LogExpert.PluginRegistry")]) as Assembly;

        Assert.That(resolved?.GetName().Name, Is.EqualTo("LogExpert.PluginRegistry"));
    }
}
