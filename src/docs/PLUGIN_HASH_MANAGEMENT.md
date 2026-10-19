# Plugin Hash Management

## Overview

LogExpert verifies built-in plugins against a table of SHA256 hashes before loading them, so a
tampered plugin DLL is rejected. `PluginValidator.GetBuiltInPluginHashes()` holds that table.

**There is nothing to run and nothing to commit.** The table is generated during every build, from
the plugin DLLs that build just produced. If you change a plugin, the next build picks it up.

## How it works

[`src/PluginRegistry/PluginHashGenerator.targets`](../PluginRegistry/PluginHashGenerator.targets)
defines a `GenerateBuiltInPluginHashes` target that runs `BeforeTargets="CoreCompile"`. It hashes
every DLL in `bin/<Configuration>/plugins` and `bin/<Configuration>/pluginsx86` (minus a list of
shipped-alongside dependencies such as `ColumnizerLib.dll`) and writes
`obj/<Configuration>/BuiltInPluginHashes.g.cs`, which is compiled into `LogExpert.PluginRegistry`.

The plugins have to exist before that target runs. `LogExpert.PluginRegistry.csproj` therefore
carries a `ProjectReference` to each plugin project marked `ReferenceOutputAssembly="false"` — build
order only, no assembly reference — so MSBuild always builds them first.

`x86` plugins are keyed with a ` (x86)` suffix, matching what `PluginValidator` looks up at runtime.

### Why it is generated rather than committed

A committed table describes whichever build produced it. Committing it creates a new commit, and the
binaries a later build produces are not guaranteed to be byte-identical to the ones that were
hashed — so the checked-in table went stale the moment it landed and never described the binaries it
shipped beside. Generating it in-build removes the problem instead of policing it: every build's
table matches that build's binaries by construction.

This also means CI has no hash step. There is nothing to verify on a pull request and nothing to
commit back, which is what previously made fork PRs unable to pass the build.

## Verifying

Hashes are only enforced in Release builds — `PluginHashCalculator.BypassHashVerification` defaults
to `true` under `DEBUG`. To confirm a Release build's table matches its own binaries:

```powershell
dotnet build src/LogExpert.sln -c Release
Get-Content src/PluginRegistry/obj/Release/BuiltInPluginHashes.g.cs
Get-FileHash bin/Release/plugins/*.dll -Algorithm SHA256
```

## Troubleshooting

### Build fails: "No built-in plugin assemblies found in ..."

The generation target found an empty plugins directory. An empty table would silently make every
built-in plugin untrusted, so the build fails instead. Check that the build-order `ProjectReference`
entries in `LogExpert.PluginRegistry.csproj` still cover every project that outputs into `plugins`
or `pluginsx86`.

### IntelliSense cannot resolve `GetBuiltInPluginHashes()`

The generated file does not exist yet on a fresh clone. Build once and it appears.

### "Plugin hash mismatch" on a user's machine after an update

Every load of `trusted-plugins.json` reconciles the built-in names and hashes with the running build's
`GetBuiltInPluginHashes()` (user-trusted entries are kept), so an upgrade does not cause this for
built-in plugins. A mismatch on a built-in plugin therefore means its bytes differ from the build
that is running: a partial or mixed install, or a modified file. Reinstall.

## Adding a plugin

Add its project to `src/LogExpert.sln` as usual, point its `OutputPath` at
`$(SolutionDir)..\bin\$(Configuration)\plugins`, then add one build-order `ProjectReference` for it
in `LogExpert.PluginRegistry.csproj`. Without that reference the plugin may build after the hash
table is generated and be missing from it.
