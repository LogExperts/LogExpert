# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

# Karpathy Guidelines

Behavioral guidelines to reduce common LLM coding mistakes.

**Tradeoff:** These guidelines bias toward caution over speed. For trivial tasks, use judgment.

## 0. Fast Path for Trivial Tasks

**For obvious, low-risk, reversible, text only ( no source code ), single-file changes, execute directly with minimal ceremony.**

- Do not create a plan, spawn or delegate to agents, run quorum review, inspect unrelated files, perform broad searches, or run Git archaeology.
- Read only the target and, when genuinely necessary, one directly relevant instruction or nearby example.
- Do not ask questions when the request and local convention determine the result; make the obvious reversible assumption.
- Use the minimum tool calls: inspect, edit, and verify once. Keep progress narration to one short update at most.
- If the preferred editing tool fails once because of an environment or tooling problem, use a safe direct fallback. Do not repeatedly debug the tool unless no safe fallback exists.
- Verification should be proportional to the change. For a simple text edit, rereading the changed content is sufficient.

This fast path overrides the planning, clarification, delegation, and quorum-review requirements below for trivial tasks. Higher-priority system, developer, safety, and explicitly invoked skill instructions still apply.

## 1. Clarify Before Executing

**Resolve ambiguity up front — before spawning agents or starting any autonomous work.**

Do this in order, then stop and ask before proceeding:

1. **Understand the request.** Read what was asked closely and state your assumptions explicitly.
2. **Inspect the codebase.** Check the relevant code, conventions, and existing patterns. Note anything that's unclear or that could be interpreted in more than one way.
3. **Ask.** Raise every consequential question now, batched together — don't pick an interpretation silently, and don't defer the question into the work itself.

Throughout:

- If multiple interpretations exist, present them — don't choose one silently.
- If a simpler approach exists, say so. Push back when warranted.
- Don't invent APIs. Confirm a function, flag, or endpoint exists — and check its signature — before calling it. If you can't verify it, say so rather than guessing.

Only once these questions are resolved do you move to execution (§4).

## 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

**Minimal comments**:

- avoid adding code comments unless necessary (i.e. only if the code itself is not self explanatory). When necessary, code comments must be as brief as possible to ensure readability and avoid overbloating.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

## 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:

- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you spot problems outside your task — dead code, bugs — mention them; don't fix or delete them unless asked.

When your changes create orphans:

- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: every changed line should trace directly to the user's request.

## 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Begin only once §1's questions are resolved. Then turn the task into a verifiable goal with an observable check. A passing test is the strongest check; a clean typecheck, running it and inspecting output, or diffing against expected output also count.

- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"
- "Update the config" → "Apply it, start the service, and confirm the setting takes effect"

For multi-step tasks, state a brief plan:

```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

When no obvious check exists (docs, exploration, some one-off scripts), ask how to verify success before starting. Don't report success you haven't actually observed.

Strong, agreed-upon criteria — settled in §1 — let you loop independently. Weak criteria ("make it work") force constant clarification, which is why §1 comes first.

## 5. Delegate Grunt Work Down a Tier

**Spend the capable model on judgment; push mechanical, low-context work to a cheaper model.**

Act as an orchestrator. Fan out grunt work — reading files, gathering context, simple searches, minor mechanical edits, other simple tool calls — to cheaper agents, then review their findings and any code changes yourself before acting on them. Keep the important and dangerous work — architecture, ambiguous decisions, risky or hard-to-reverse edits, final review — on the more capable model.

When delegation is available, use a less capable, cheaper model for genuinely simple, mechanical, low-risk work. Keep tasks requiring substantial context or judgment on the primary, more capable model.

Match the model to the task: pick the cheapest tier that can do the job well, and skip an intermediate tier when the work is simple enough for a lower one.

Guidelines:

- Only delegate work that is genuinely low-context and low-risk. If doing it well requires the capable model's judgment, keep it.
- Give each agent a self-contained task with clear success criteria (per §4) so you can verify its output without redoing the work.
- Never merge a delegated finding or edit unblocked — review it first. The cheaper model did the legwork; you own the decision.

## 6. Quorum Review for Big Changes

**For bigger changes, three agents check the work and two of three must agree before it counts as a success.**

This applies to any change that is large, or that spans multiple files. For trivial, single-file edits, skip it and use §4's normal verification.

- Spawn **three independent agents** to review the completed change, each judging against the success criteria settled in §1/§4.
- **Vary each agent's prompt slightly so they are not identical.** Give them the same context and criteria, but frame each reviewer differently — a distinct persona or emphasis (e.g. one as a correctness-focused engineer, one probing edge cases, one checking that it actually satisfies the request). Don't hand all three the same "You are a systems engineer…" opening.
- The change is a success only when **at least two of the three agree** it is correct and complete.
- If the vote fails (two or more say it's wrong or incomplete), treat it as not done: address their objections, then re-run the quorum.
- Keep their judgments independent so the vote is meaningful, and read their reasoning yourself before accepting the verdict.

## 7. Reporting Results

- Keep straightforward findings and small fixes in chat.
- If a review has four or more substantive findings, or the detailed response would exceed roughly 25 lines, write the full results to a clearly named Markdown file in the repository.
- In chat, provide only a short summary and a link to that report.
- Do not create a report file for a simple bug, small change, or brief explanation.

## Project Overview

LogExpert is a Windows log file viewer and analyzer built with C# and Windows Forms. It's a GUI replacement for the Unix `tail` command with extensive features including tail mode, filtering, bookmarks, highlighting, and a plugin-based architecture for parsing custom log formats.

**Key Technologies:**
- .NET 10.0 (Windows target framework)
- Windows Forms for UI
- Nuke Build System with MSBuild
- NUnit for testing
- Plugin-based architecture

## Environment

This is a Windows .NET/WinForms repo. Python is often unavailable — prefer PowerShell, `tshark`, and built-in tooling. Check whether `gh` CLI (and any other external tool) is actually installed before building a plan around it. The active GitHub account is `brunerpat`.

### Line Endings

The working tree is CRLF on Windows (only `usedComponents.json` is pinned to LF via [.gitattributes](.gitattributes); there is no global normalization). Never use `sed`-based or other text-tool renames that strip line endings — they produce a whole-file diff of pure line-ending churn. After any bulk rename, verify no references were left unrenamed (a missed reference will break the build).

## Testing

Follow TDD for all bug fixes: write a failing test that reproduces the issue first, then fix, then verify the **full suite** passes — not just the new test in isolation (timing/filesystem tests can pass alone but fail alongside the suite).

## Documentation

Docs must be code-grounded and reconciled against the actual source and live config, then reader-tested before delivery. Don't document from assumption.

## Refactoring

For "behavior-preserving" refactors, explicitly confirm equivalence and flag any unintended logic changes before applying.

## Build Commands

### Using Nuke Build (Recommended)

```powershell
# Build the solution
./build.ps1

# Clean and build
./build.ps1 --target Clean Compile

# Run tests
./build.ps1 --target Test

# Full release build with packages
./build.ps1 --target Clean Pack CreateSetup --configuration Release
```

#### How the Nuke build works

- `build.ps1` bootstraps and runs the Nuke build defined in [build/Build.cs](build/Build.cs). Targets and their dependencies are declared there in C#.
- **Default target is `Test`** (which depends on `Compile`), so a bare `./build.ps1` restores, compiles, and runs the tests.
- Select targets with `--target <Name>` (space-separated for multiple). Common targets: `Clean`, `Restore`, `Compile`, `Test`, `Pack`, `CreateSetup`, `Publish`.
- List all available targets: `./build.ps1 --help`
- Preview the execution plan without running it: `./build.ps1 --plan`
- Pick build configuration with `--configuration Debug|Release` (defaults to `Debug` locally, `Release` on CI).

### Using .NET CLI Directly

```bash
# From src/ directory
dotnet restore
dotnet build --no-restore
dotnet test --no-build --verbosity normal

# Run specific test project
dotnet test src/LogExpert.Tests/LogExpert.Tests.csproj
```

### Important Build Notes

- **Windows-only**: This project requires Windows and .NET 10.0.100 SDK (specified in [global.json](global.json))
- **Cannot build on Linux/macOS**: Uses Windows Desktop SDK and Windows Forms
- Nuke build automatically downloads the correct .NET SDK version if needed
- Output directory: `bin/(Debug|Release)/`

## Architecture

### High-Level Structure

The codebase follows a modular architecture with clear separation of concerns:

```
LogExpert/
├── LogExpert/              # Main application entry point and UI orchestration
├── LogExpert.Core/         # Core business logic, log reading, filtering
├── LogExpert.UI/           # Windows Forms UI components and dialogs
├── LogExpert.Resources/    # Localization resources
├── LogExpert.Configuration/# Configuration management
├── ColumnizerLib/          # Plugin interface definitions
├── PluginRegistry/         # Plugin discovery and security
└── Columnizers/            # Built-in columnizer plugins (CSV, JSON, Regex, etc.)
```

### Key Architectural Components

#### 1. Single Instance Mode with IPC
- Application uses a Mutex to ensure single instance per session
- Named pipes (`LogExpertInstanceMutex{sessionId}`) for inter-process communication
- Secondary instances send file paths to primary instance via JSON over named pipes
- See [Program.cs](src/LogExpert/Program.cs) for implementation

#### 2. Plugin System
- **Columnizers** (`ILogLineColumnizer`): Parse log lines into columns
- **File System Plugins** (`IFileSystemPlugin`): Support non-local file sources (e.g., SFTP)
- **Context Menu Plugins** (`IContextMenuEntry`): Add custom menu items
- **Keyword Actions** (`IKeywordAction`): React to keywords in logs
- Plugin discovery happens at startup via `PluginRegistry`
- Plugins are loaded from `plugins/` and `pluginsx86/` directories
- Security: Plugin hashes are verified against generated hashes (Release builds only)
- See [PLUGIN_DEVELOPMENT_GUIDE.md](src/docs/PLUGIN_DEVELOPMENT_GUIDE.md) for details

#### 3. Log File Reading
- Abstract base class: `PositionAwareStreamReaderBase`
- Implementations: `PositionAwareStreamReaderSystem`, `PositionAwareStreamReaderLegacy`
- Uses buffered streams for efficient reading of large files
- Supports encoding detection (UTF-8, UTF-16, UTF-32 with BOM)
- Position tracking for tail mode and seeking
- See [src/LogExpert.Core/Classes/Log/](src/LogExpert.Core/Classes/Log/) for implementations

#### 4. Configuration Management
- Centralized via `ConfigManager.Instance`
- Initialized with application startup path and screen information
- Supports import/export of settings
- Persists user preferences, columnizer history, highlight masks, etc.
- Configuration stored in application startup directory (portable mode)

#### 5. Windows Forms UI Architecture
- MDI interface with tab support via `AbstractLogTabWindow`
- Main window created in [LogTabWindow.cs](src/LogExpert.UI/Dialogs/LogTabWindow/)
- Custom controls: `BufferedDataGridView`, `LogTabControl`, `DateTimeDragControl`
- High DPI considerations: Avoid `AutoScaleMode` and `AutoScaleDimensions` on individual controls
- Dark mode support via `Application.SetColorMode()`

### Critical Files and Their Purposes

- [Program.cs](src/LogExpert/Program.cs) - Application entry point, IPC setup, single instance handling
- [AbstractLogTabWindow.cs](src/LogExpert.UI/Extensions/LogWindow/AbstractLogTabWindow.cs) - Main window factory and orchestration
- [ILogLineColumnizer.cs](src/ColumnizerLib/ILogLineColumnizer.cs) - Core plugin interface for columnizers
- [ColumnizerPicker.cs](src/LogExpert.Core/Classes/Columnizer/ColumnizerPicker.cs) - Automatic columnizer detection
- [PluginRegistry.cs](src/PluginRegistry/) - Plugin discovery and security verification
- [ConfigManager.cs](src/LogExpert.Configuration/) - Configuration persistence and management
- [LogBuffer.cs](src/LogExpert.Core/Classes/Log/LogBuffer.cs) - In-memory log line caching

## Development Workflow

### Adding a New Columnizer Plugin

1. Create new project in `src/` following naming pattern `*Columnizer`
2. Add project reference to `ColumnizerLib`
3. Implement `ILogLineColumnizer` interface
4. Add project to `src/LogExpert.sln`
5. Create corresponding test project in `Tests/` folder
6. Plugin will be auto-discovered at runtime from output directory

### Modifying Core Log Reading Logic

- Core reading classes are in [src/LogExpert.Core/Classes/Log/](src/LogExpert.Core/Classes/Log/)
- Inherit from `PositionAwareStreamReaderBase` for custom stream readers
- Key methods to implement: `ReadLine()`, `Position` property, `Seek()`
- Always maintain position tracking for tail mode support

### Working with Windows Forms UI

- UI components in `LogExpert.UI` project
- Follow existing High DPI patterns (no AutoScale on controls)
- Test with both light and dark mode (see `SetColorMode()` in Program.cs; the `ColorMode` preference is Light/Dark/System)
- Use localization resources from `LogExpert.Resources` project
- Windows Forms designer files: `*.designer.cs`

### Test Setup & Conventions

- Unit tests use NUnit framework with Moq for mocking
- Test projects follow naming pattern `*.Tests`
- Test data stored in `TestData/` directories within test projects
- Run all tests: `./build.ps1 --target Test`
- Run specific test: `dotnet test src/LogExpert.Tests/LogExpert.Tests.csproj`

## Important Patterns and Conventions

### Code Style
- Nullable reference types enabled (`<Nullable>enable</Nullable>`)
- `.editorconfig` is the source of truth for formatting and analyzer severities — match it rather than imposing your own style; don't fight or override its rules
- ImplicitUsings enabled
- Assembly signing enabled (Key.snk)

### Configuration Files
- **Directory.Build.props** - Common MSBuild properties for all projects
- **Directory.Packages.props** - Centralized NuGet package version management
- **global.json** - .NET SDK version pinning (10.0.100)
- **.editorconfig** - Code style and analysis rules

### Project Organization
- Solution folders: "Columnizers", "Tests", "docs", "setup"
- Test projects nested under "Tests" solution folder
- Columnizer projects nested under "Columnizers" solution folder
- Documentation in `src/docs/` included in solution

### Git Workflow
- Default branch: `Development` (use for PRs)
- Commit format: **Do not** add a `Co-Authored-By` trailer (or any other agent/AI attribution) to commit messages
- GitHub Actions run on push to Development branch
- AppVeyor is the second CI for builds and artifact creation. It is currently disabled and needs to be re-added alongside GitHub Actions

## Plugin Security System

- SHA256 hashes of the built-in plugins are generated during **every** build by
  [PluginHashGenerator.targets](src/PluginRegistry/PluginHashGenerator.targets), which runs before
  `LogExpert.PluginRegistry` compiles and writes `obj/<Configuration>/BuiltInPluginHashes.g.cs`
- Build-order-only `ProjectReference` entries in `LogExpert.PluginRegistry.csproj` guarantee the
  plugins are built first
- The table is **not** committed and there is no CI step for it — nothing to regenerate by hand,
  nothing to verify, nothing to commit back
- At runtime, `PluginRegistry` verifies plugin hashes before loading; verification is skipped in
  Debug builds (`PluginHashCalculator.BypassHashVerification`)
- Users can trust new plugins via UI dialog
- See [PLUGIN_HASH_MANAGEMENT.md](src/docs/PLUGIN_HASH_MANAGEMENT.md)

## Common Gotchas

1. **Cross-platform builds fail**: This is Windows-only. Don't attempt Linux/macOS builds.
2. **SDK version mismatch**: Must use .NET 10.0.100 SDK (specified in global.json)
3. **Plugin not loading**: Check output directory - plugins must be in `plugins/` or `pluginsx86/`
4. **High DPI issues**: Never use AutoScaleMode on individual controls, only on forms
5. **IPC failures**: Named pipes require proper Windows permissions and session isolation
6. **Encoding detection**: BOM-less files default to encoding from EncodingOptions
7. **Plugin hashes**: Only verified in Release builds; Debug builds skip verification

## Dont Do that

Hard rules — never do these:

- **Don't add a `Co-Authored-By` trailer** (or any other agent/AI attribution) to commit messages.
- **Don't attempt Linux/macOS builds.** This is Windows-only (Windows Desktop SDK + Windows Forms).
- **Don't use `AutoScaleMode` or `AutoScaleDimensions` on individual controls** — only on forms (High DPI).
- **Don't override or fight `.editorconfig` rules** — it is the source of truth for formatting and analyzer severities.

## Key Dependencies

- **NLog**: Logging framework
- **Newtonsoft.Json**: JSON serialization (legacy, but widely used)
- **CsvHelper**: CSV parsing in CsvColumnizer
- **SSH.NET**: SFTP support in SftpFileSystem plugins
- **DockPanelSuite**: Docking panel UI controls
- **Moq/NUnit**: Testing frameworks

## References

- Main README: [README.md](README.md)
- Plugin Development: [PLUGIN_DEVELOPMENT_GUIDE.md](src/docs/PLUGIN_DEVELOPMENT_GUIDE.md)
- Plugin Hash System: [PLUGIN_HASH_MANAGEMENT.md](src/docs/PLUGIN_HASH_MANAGEMENT.md)
- GitHub Wiki: https://github.com/LogExperts/LogExpert/wiki
- Discord: https://discord.gg/SjxkuckRe9

## Agent skills

### Issue tracker

Issues are tracked in `LogExperts/LogExpert` GitHub Issues, read-only for skills via the `gh` CLI — analysis only; only humans post. See `docs/agents/issue-tracker.md`.

### Triage labels

Canonical triage roles map to repo labels (`needs-info` → `question`; the rest use default names), used to read/filter issues — skills don't apply labels. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context: one `CONTEXT.md` + `docs/adr/` at the repo root. See `docs/agents/domain.md`.

# Update Rules File
To update this file, ensure that all sections are kept current with the latest architectural decisions, build processes, and development workflows. Follow these guidelines:
- Review and update build commands if there are changes in the build system.
- Reflect any architectural changes in the "Architecture" section.
- Keep development workflow steps accurate for new contributors.
- Regularly verify links to other documentation files.
- Maintain clarity and conciseness for ease of understanding by new developers.
- Use consistent formatting throughout the document.
- Add new sections as needed for significant changes in the project structure or processes.
- Ensure all technical terms are explained or linked to relevant documentation.
- Periodically review for outdated information and remove or update as necessary.
- If told to not do something, ensure this is also added to the "Dont Do that" section.