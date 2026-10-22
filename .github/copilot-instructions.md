# GitHub Copilot Instructions for LogExpert

## 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them - don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

## 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

## 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it - don't delete it.

When your changes create orphans:
- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

## 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:
- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:
```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

## Repository Overview

**LogExpert** is a Windows-based log file tail and analysis application written in C#. It's a GUI replacement for the Unix tail command, originally from CodePlex, with extensive features for viewing, analyzing, and filtering log files.

### Key Features
- Tail mode for real-time log monitoring
- MDI-Interface with tabs for multiple files
- Search functionality including RegEx support
- Bookmarks and highlighting capabilities
- Flexible filter views with filter-to-tab functionality
- Columnizer plugins for parsing structured logs
- Unicode support and log4j XML file support
- 3rd party plugin architecture
- Automatic columnizer detection (experimental)
- Serilog.Formatting.Compact format support

### Technology Stack
- **Primary Language**: C# (.NET 10.0-windows target framework)
- **UI Framework**: Windows Forms 
- **Build System**: Nuke Build System with MSBuild
- **Target Platform**: Windows (requires Windows-specific dependencies)
- **Package Management**: NuGet with central package management
- **Testing**: NUnit framework
- **CI/CD**: GitHub Actions + AppVeyor

## High-Level Repository Information

- **Repository Size**: Medium (~26 source projects)
- **Project Type**: Desktop Application (Windows Forms)
- **Architecture**: Plugin-based architecture with columnizers
- **Main Entry Point**: `src/LogExpert/Program.cs`
- **Main Solution**: `src/LogExpert.sln`

## Build Instructions

### Prerequisites
**CRITICAL**: This project requires Windows development environment and .NET 10.0.100 SDK or compatible.

### Environment Setup
1. **Install .NET SDK**: Project requires .NET 10.0.100 SDK (specified in `global.json`)
2. **Windows Environment**: Build targets `net10.0-windows` and uses Windows Forms
3. **Visual Studio**: Recommended Visual Studio 2026+ or Visual Studio Code with C# extension
4. **Optional Dependencies**:
   - Chocolatey (for packaging)
   - Inno Setup 5 or 6 (for setup creation)

### Build Commands

#### Using Nuke Build System (Recommended)
```bash
# Windows Command Prompt/PowerShell
./build.ps1

# Cross-platform (Linux/macOS) - Note: Limited functionality
./build.sh
```

#### Common Nuke Build Targets
```bash
# Clean and build
./build.ps1 --target Clean Compile

# Run tests
./build.ps1 --target Test

# Create packages
./build.ps1 --target Pack

# Full release build with setup
./build.ps1 --target Clean Pack CreateSetup --configuration Release
```

#### Direct .NET Commands
```bash
# From src/ directory
dotnet restore
dotnet build --no-restore
dotnet test --no-build --verbosity normal
```

### Known Build Issues and Workarounds

1. **Cross-Platform Limitations**: 
   - Linux/macOS builds will fail due to missing Windows Desktop SDK components
   - Error: "Microsoft.NET.Sdk.WindowsDesktop/targets" not found
   - **Workaround**: Use Windows environment or Windows Subsystem for Linux with proper .NET Windows SDK

2. **.NET Version Mismatch**:
   - Project requires .NET 10.0.100 but may encounter .NET 8.0 environments
   - **Workaround**: Nuke build system automatically downloads correct SDK version

3. **Build Timing**:
   - Full build: ~2-5 minutes on modern hardware
   - Test execution: ~30 seconds to 2 minutes
   - Package creation: Additional 1-3 minutes

### Validation Steps
Always run these validation steps after making changes:
1. `./build.ps1 --target Clean Compile` (ensures clean build)
2. `./build.ps1 --target Test` (runs all unit tests)
3. Review build output in `bin/` directory
4. Check for warnings in build output

## Project Layout and Architecture

### Repository Structure
```
LogExpert/
├── .github/                    # GitHub Actions workflows
│   └── workflows/             # build_dotnet.yml, test_dotnet.yml
├── src/                       # Main source directory
│   ├── LogExpert.sln          # Main solution file
│   ├── LogExpert/             # Main application project
│   ├── LogExpert.Core/        # Core functionality library
│   ├── LogExpert.UI/          # UI components library
│   ├── LogExpert.Resources/   # Resource files
│   ├── ColumnizerLib/         # Plugin interface library
│   ├── Columnizers/           # Built-in columnizer plugins
│   │   ├── CsvColumnizer/
│   │   ├── JsonColumnizer/
│   │   ├── RegexColumnizer/
│   │   └── ...                # Other columnizers
│   ├── Tests/                 # Test projects
│   │   ├── LogExpert.Tests/
│   │   ├── ColumnizerLib.UnitTests/
│   │   └── RegexColumnizer.UnitTests/
│   └── Solution Items/        # Shared configuration files
├── build/                     # Nuke build system
│   ├── Build.cs              # Main build script
│   └── _build.csproj         # Build project file
├── chocolatey/               # Chocolatey package configuration
├── lib/                      # External libraries
├── global.json              # .NET SDK version pinning
├── GitVersion.yml           # Version configuration
├── Directory.Build.props    # MSBuild properties
└── Directory.Packages.props # NuGet package versions
```

### Key Configuration Files
- **`global.json`**: Specifies required .NET SDK version (9.0.301)
- **`src/Directory.Build.props`**: Common MSBuild properties for all projects
- **`src/Directory.Packages.props`**: Centralized NuGet package version management
- **`.editorconfig`**: Code style and formatting rules (comprehensive 4000+ line config)
- **`GitVersion.yml`**: Semantic versioning configuration
- **`appveyor.yml`**: AppVeyor CI configuration

### Architectural Components

#### Main Application (`src/LogExpert/`)
- **`Program.cs`**: Application entry point with IPC for single-instance mode
- **Target Framework**: `net8.0-windows`
- **Dependencies**: LogExpert.UI, LogExpert.Core, ColumnizerLib, PluginRegistry

#### Core Libraries
- **`LogExpert.Core`**: Core business logic and interfaces
- **`LogExpert.UI`**: Windows Forms UI components
- **`LogExpert.Resources`**: Localization and resource files
- **`ColumnizerLib`**: Plugin interface definitions

#### Plugin System
- **Columnizers**: Parse log lines into columns (CSV, JSON, RegEx, etc.)
- **File System Plugins**: Support for different file sources (local, SFTP)
- **Plugin Discovery**: Automatic plugin loading from application directory

### CI/CD Pipeline

#### GitHub Actions
1. **`.github/workflows/build_dotnet.yml`**: 
   - Triggers on PR to Development branch
   - Builds Debug and Release configurations
   - Uploads build artifacts
   - Uses windows-latest runner

2. **`.github/workflows/test_dotnet.yml`**:
   - Runs on push to Development branch
   - Executes unit tests
   - Uses .NET 10.0.x SDK

#### AppVeyor Integration
- **`appveyor.yml`**: Legacy CI configuration
- Builds packages and publishes artifacts
- Creates setup executables with Inno Setup

### Testing Strategy
- **Unit Tests**: Located in `*Tests.csproj` projects
- **Test Frameworks**: NUnit with Moq for mocking
- **Test Data**: Located in `TestData/` directories within test projects
- **Coverage**: Focus on core functionality and columnizer plugins

### Dependencies and Libraries
Key external dependencies (managed via Directory.Packages.props):
- **NLog**: Logging framework
- **Newtonsoft.Json**: JSON processing
- **CsvHelper**: CSV file processing
- **SSH.NET**: SFTP file system support
- **DockPanelSuite**: UI docking panels
- **NUnit/Moq**: Testing frameworks

## Agent Guidance

### Making Code Changes
1. **Always build before changing**: Run `./build.ps1 --target Clean Compile Test` to establish baseline
2. **Follow existing patterns**: Study similar implementations in the codebase
3. **Respect architecture**: Use plugin interfaces for extensibility
4. **Code style**: Follow `.editorconfig` rules (extensive configuration provided)
5. **Null safety**: Project uses nullable reference types (`<Nullable>enable</Nullable>`)

### Common Development Tasks

#### Adding New Columnizer
1. Create new project in `src/` directory following naming pattern `*Columnizer`
2. Implement `ILogLineColumnizer` interface from `ColumnizerLib`
3. Add project reference to main solution
4. Add unit tests in corresponding `*Tests` project

#### Modifying UI Components
1. UI components are in `LogExpert.UI` project
2. Follow Windows Forms patterns
3. Be aware of High DPI considerations (documented in README)
4. Test on different Windows versions if possible

#### Adding Dependencies
1. Update `src/Directory.Packages.props` for version management
2. Add `<PackageReference>` in specific project files
3. Ensure compatibility with .NET 10.0 target framework

### Build Troubleshooting
- **Missing Windows SDK**: Ensure Windows development environment
- **Version conflicts**: Check `global.json` and upgrade SDK if needed
- **Plugin loading issues**: Verify plugins are copied to output directory
- **Test failures**: Check test data file paths and Windows-specific assumptions

### Important Notes
- **Windows-only**: This is a Windows-specific application using Windows Forms
- **Plugin architecture**: Extensibility through columnizer and file system plugins
- **Single instance**: Application uses named pipes for IPC between instances
- **Legacy codebase**: Contains patterns from .NET Framework era, being modernized
- **Comprehensive configuration**: Very detailed .editorconfig and analysis rules

### Trust These Instructions
These instructions are comprehensive and tested. Only search for additional information if:
1. Instructions appear incomplete for your specific task
2. You encounter errors not covered in the troubleshooting section
3. You need to understand implementation details not covered here

The build system, project structure, and development patterns described here are accurate as of the current codebase state.