using System.Text.Json;

using NUnit.Framework;

namespace LogExpert.Tests.Packaging;

/// <summary>
/// Regression guard for #634 (follow-tail dead in installed builds). The Inno Setup
/// <c>[Files]</c> list was a hand-maintained allowlist and had drifted: runtime assemblies that
/// the 1.40 refactor added (LogExpert.Audio + NAudio*, CommunityToolkit.HighPerformance, several
/// System.* libs) existed in the build output and in LogExpert.deps.json but were NOT shipped by
/// the installer. On the first tailed line the worker thread JIT-loaded LogExpert.Audio, threw
/// FileNotFoundException, and died — so no file ever updated again once installed.
///
/// This test asserts the installer ships every runtime assembly declared in LogExpert.deps.json,
/// so the file list can never silently drift away from the actual dependency closure again.
/// </summary>
[TestFixture]
public class InstallerCoverageTests
{
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void Installer_ShipsEveryRuntimeAssembly_DeclaredInDepsJson ()
    {
        var repoRoot = FindRepoRoot();
        if (repoRoot == null)
        {
            Assert.Ignore("Repo root (src/setup/LogExpertInstaller.iss) not found from test directory.");
            return;
        }

        var depsJson = FindAppDepsJson(repoRoot);
        if (depsJson == null)
        {
            Assert.Ignore("LogExpert.deps.json not found under bin/Release or bin/Debug; build the app first.");
            return;
        }

        var required = GetRuntimeAssemblyFileNames(depsJson);
        Assert.That(required, Is.Not.Empty, "Failed to parse any runtime assemblies from deps.json.");

        var shipped = GetInstallerShippedFileNames(Path.Join(repoRoot, "src", "setup"));

        var missing = required
            .Where(name => !shipped.Contains(name))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.That(missing, Is.Empty,
            $"The installer omits {missing.Count} runtime assemblies that LogExpert.deps.json requires. " +
            $"Installed builds will fail when these are first loaded. Missing: {string.Join(", ", missing)}");
    }

    private static string FindRepoRoot ()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Join(dir.FullName, "src", "setup", "LogExpertInstaller.iss")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }

    private static string FindAppDepsJson (string repoRoot)
    {
        foreach (var config in new[] { "Release", "Debug" })
        {
            var candidate = Path.Join(repoRoot, "bin", config, "LogExpert.deps.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts the set of runtime assembly file names (the <c>runtime</c> entries under every
    /// target/library) from a .deps.json file. Satellite resource assemblies are intentionally
    /// excluded — this guards the primary runtime closure where the #634 drift happened.
    /// </summary>
    private static HashSet<string> GetRuntimeAssemblyFileNames (string depsJsonPath)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var doc = JsonDocument.Parse(File.ReadAllText(depsJsonPath));
        if (!doc.RootElement.TryGetProperty("targets", out var targets))
        {
            return result;
        }

        foreach (var target in targets.EnumerateObject())
        {
            foreach (var library in target.Value.EnumerateObject())
            {
                if (!library.Value.TryGetProperty("runtime", out var runtime))
                {
                    continue;
                }

                foreach (var name in runtime.EnumerateObject().Select(f => Path.GetFileName(f.Name)).Where(name => name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)))
                {
                    _ = result.Add(name);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Collects every file name referenced by a <c>Source:</c> directive in the installer script,
    /// following <c>#include</c> directives (so a generated file list is honoured too).
    /// </summary>
    private static HashSet<string> GetInstallerShippedFileNames (string setupDir)
    {
        var shipped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CollectShippedFromIss(Path.Join(setupDir, "LogExpertInstaller.iss"), setupDir, shipped, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        return shipped;
    }

    private static void CollectShippedFromIss (string issPath, string setupDir, HashSet<string> shipped, HashSet<string> visited)
    {
        if (!File.Exists(issPath) || !visited.Add(issPath))
        {
            return;
        }

        foreach (var line in File.ReadAllLines(issPath).Select(line => line.Trim()))
        {
            if (line.StartsWith(';'))
            {
                continue;
            }

            if (line.StartsWith("#include", StringComparison.OrdinalIgnoreCase))
            {
                var included = line.Split('"').ElementAtOrDefault(1);
                if (!string.IsNullOrWhiteSpace(included))
                {
                    CollectShippedFromIss(Path.Join(setupDir, included), setupDir, shipped, visited);
                }

                continue;
            }

            var sourceIndex = line.IndexOf("Source:", StringComparison.OrdinalIgnoreCase);
            if (sourceIndex < 0)
            {
                continue;
            }

            var quoteStart = line.IndexOf('"', sourceIndex);
            var quoteEnd = quoteStart >= 0 ? line.IndexOf('"', quoteStart + 1) : -1;
            if (quoteStart < 0 || quoteEnd < 0)
            {
                continue;
            }

            var path = line.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
            var fileName = path.Replace('/', '\\').Split('\\').LastOrDefault();
            if (!(string.IsNullOrWhiteSpace(fileName) || fileName.Contains('*', StringComparison.OrdinalIgnoreCase)))
            {
                _ = shipped.Add(fileName);
            }
        }
    }
}
