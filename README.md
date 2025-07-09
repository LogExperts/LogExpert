# LogExpert [![.NET](https://github.com/LogExperts/LogExpert/actions/workflows/test_dotnet.yml/badge.svg)](https://github.com/LogExperts/LogExpert/actions/workflows/test_dotnet.yml)

This is a clone from (no longer exists) https://logexpert.codeplex.com/

# Overview
LogExpert is a Windows tail program (a GUI replacement for the Unix tail command).

Summary of (most) features:

* Tail mode
* MDI-Interface with Tabs
* Search function (including RegEx)
* Bookmarks
* A very flexible filter view and possibility to filter to tab
* Highlighting lines via search criteria
* Triggers (e.g. create Bookmark or execute a plugin) via search criteria
* Columnizers: Plugins which split log lines into columns
* Unicode support
* log4j XML file support
* 3rd party plugin support
* Plugin API for more log file data sources
* Automatical determine columnizer with given file name and content (Experimental)
* Serilog.Formatting.Compact format support (Experimental)
* Portable (all options / settings saved in application startup directory)

# Download
Follow the [Link](https://github.com/LogExperts/LogExpert/releases/latest) and download the latest package. Just extract it where you want and execute the application or download the Setup and install it

Or Install via chocolatey

```choco install logexpert```

Requirements
- https://dotnet.microsoft.com/en-us/download/dotnet/8.0
- .NET 8 (https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-8.0.13-windows-x64-installer or https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-8.0.13-windows-x86-installer) 
- ASP.NET Core Runtime (https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-aspnetcore-8.0.13-windows-x64-installer, https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-aspnetcore-8.0.13-windows-x86-installer or https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-aspnetcore-8.0.13-windows-hosting-bundle-installer)

## CI
This is a continous integration build. So always the latest and greates changes. It should be stable but no promises. Can be viewed as Beta.

[CI Download](https://ci.appveyor.com/project/Zarunbal/logexpert)

# How to Build

- Clone / Fork / Download the source code
- Open the Solution (src/LogExpert.sln) with Visual Studio 2017 (e.g. Community Edition)
- Restore Nuget Packages on Solution
- Build
- The output is under bin/(Debug/Release)/

Nuke.build Requirements
- Chocolatey must be installed
- Optional for Setup Inno Script 5 or 6

# Pull Request
- Use Development branch as target

# FAQ / HELP / Informations / Examples
Please checkout the wiki for FAQ / HELP / Informations / Examples

# High DPI
- dont use AutoScaleMode for single GUI controls like Buttons etc.
- dont use AutoScaleDimensions for single GUI controls like Buttons etc.

https://github.com/LogExperts/LogExpert/wiki

# Discord Server
https://discord.gg/SjxkuckRe9

## Credits
### Contributors

This project exists thanks to all the people who contribute.
<a href="https://github.com/LogExperts/LogExpert/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=LogExperts/LogExpert" />
</a>

Made with [contrib.rocks](https://contrib.rocks).
