# CLAUDE.md

LogExpert: Windows log viewer/tail tool in C# / Windows Forms (.NET 10, `net10.0-windows`), Nuke build, NUnit + Moq tests, plugin-based columnizers. Main solution: `src/LogExpert.sln`. Entry point: `src/LogExpert/Program.cs`.

Before planning, delegating or reviewing a multi-file change, read [.github/agent-guidelines.md](.github/agent-guidelines.md) (clarify-first, simplicity, delegation, quorum-of-3 review).
For repo layout, CI pipelines and the full architecture overview, see [.github/copilot-instructions.md](.github/copilot-instructions.md).

## Environment

- Windows only. Python: `py -I`. Python is often unavailable; prefer PowerShell and built-in tooling.
- Check that `gh` (and any other external tool) is installed before planning around it. Active GitHub account: `brunerpat`.
- Line endings: the working tree is CRLF (only `usedComponents.json` is pinned to LF in `.gitattributes`). Never use `sed` or other text tools that strip CRs; they produce whole-file churn. After a bulk rename, check no reference was missed.

## Build and test

```powershell
./build.ps1                                   # default target Test (restore, compile, test)
./build.ps1 --target Clean Compile
./build.ps1 --target Test
./build.ps1 --target Clean Pack CreateSetup --configuration Release
./build.ps1 --help                            # list targets; --plan previews
```

Targets live in `build/Build.cs`. Directly, from `src/`: `dotnet restore`, `dotnet build --no-restore`, `dotnet test --no-build --verbosity normal`, or one project: `dotnet test src/LogExpert.Tests/LogExpert.Tests.csproj`. Output goes to `bin/(Debug|Release)/`. SDK pinned in `global.json`.

Local checks that CI also runs:

- `pwsh -File tools/Test-ResxKeyParity.ps1`: resource keys must match across `Resources.resx`, `Resources.de.resx`, `Resources.zh-CN.resx` and `Resources.Designer.cs`. Add every new string to all three resx files.
- `dotnet format whitespace src/LogExpert.sln --verify-no-changes --include <changed .cs files>`: CI checks only the files a PR changes, because the existing code has many old violations.

Testing: TDD for bug fixes (failing test first). Then run the **full suite**, not only the new test; timing and filesystem tests can pass alone and fail together.

### Known flaky UI tests (on `Development` as of 2026-09-26, @ d9845e24)

Don't chase these as regressions; check whether they're fixed before relying on this.

- `MarkerWindowTests.InactiveWindow_KeepsItsOwnSearchWhenAnotherWindowSearches` **hangs** the test host (0 % CPU, a LogExpert window left open) in about 1/3 to 1/2 of runs when another LogTabWindow test runs before it in the same process. It passes alone.
- `SelectionPainterTests.PaintedBlock_HasNoInternalEdge_AndErasesOldEdgesAfterSelectionChanges` **fails** deterministically (pixel colour assertion).
- Run UI tests with `--blame-hang --blame-hang-timeout 90s --blame-hang-dump-type none` so a hang names the test. For a clean full run, add `--filter "FullyQualifiedName!~InactiveWindow_KeepsItsOwnSearch"` and treat the painter failure as known.

## Code style and review

- `src/.editorconfig` is the source of truth for formatting and analyzer severities. Match it; don't override it.
- Nullable reference types and ImplicitUsings are on; assemblies are signed (`Key.snk`). Package versions are in `src/Directory.Packages.props`.
- When reviewing code, apply [docs/CODING_STANDARDS.md](docs/CODING_STANDARDS.md).
- Before reviewing a PR, check for `docs/improve/review-pr-<n>.md`. If it exists, build on it instead of starting over. Save each finished review there.

## Architecture pointers

- `src/LogExpert.Core/Classes/Log/`: log reading (`PositionAwareStreamReaderBase` and its implementations, `LogBuffer`). Keep position tracking intact for tail mode.
- `src/LogExpert.UI/`: WinForms UI. Main window: `src/LogExpert.UI/Dialogs/LogTabWindow/`. Strings come from `src/LogExpert.Resources`.
- `src/ColumnizerLib/ILogLineColumnizer.cs`: plugin interface. New columnizer: a `*Columnizer` project referencing `ColumnizerLib`, added to `src/LogExpert.sln`, with a test project.
- `src/LogExpert/Program.cs`: single-instance mutex plus named-pipe IPC (`LogExpertInstanceMutex{sessionId}`).
- Plugin hashes: `src/PluginRegistry/PluginHashGenerator.targets` writes `obj/<Configuration>/BuiltInPluginHashes.g.cs` on every build. Build-order-only `ProjectReference`s in `src/PluginRegistry/LogExpert.PluginRegistry.csproj` make sure plugins build first. The table is not committed: nothing to regenerate, verify or commit. Verification is skipped in Debug (`PluginHashCalculator.BypassHashVerification`). See `src/docs/PLUGIN_HASH_MANAGEMENT.md`.

## Common gotchas

1. Linux/macOS builds fail (Windows Desktop SDK).
2. The SDK version must match `global.json`.
3. Plugin not loading: plugins must be in `plugins/` or `pluginsx86/` next to the exe.
4. High DPI: `AutoScaleMode` belongs on forms only, never on individual controls.
5. IPC failures: named pipes need the right Windows permissions and session isolation.
6. BOM-less files fall back to the encoding from `EncodingOptions`.
7. Plugin hashes are only verified in Release builds.
8. Test dark mode as well as light mode (`ColorMode` preference: Light/Dark/System; see `SetColorMode()` in `Program.cs`).

## Dont Do that

Hard rules. Never do these:

- **Don't add a `Co-Authored-By` trailer** (or any other agent/AI attribution) to commit messages. The `.githooks/commit-msg` hook strips them; enable it with `git config core.hooksPath .githooks`.
- **Don't attempt Linux/macOS builds.**
- **Don't use `AutoScaleMode` or `AutoScaleDimensions` on individual controls**, only on forms.
- **Don't override or fight `src/.editorconfig` rules.**

## Git and CI

- Default branch `Development`; PRs target it. GitHub Actions run on push and PR to `Development`. AppVeyor is disabled for now.
- Issues are read-only for agents: see `docs/agents/issue-tracker.md`, `docs/agents/triage-labels.md`, and `docs/agents/domain.md` (single `CONTEXT.md` + `docs/adr/`).

When told not to do something, add it to "Dont Do that".
