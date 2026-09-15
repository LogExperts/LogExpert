# LogExpert [![.NET](https://github.com/LogExperts/LogExpert/actions/workflows/build_dotnet.yml/badge.svg)](https://github.com/LogExperts/LogExpert/actions/workflows/build_dotnet.yml) [![Release](https://github.com/LogExperts/LogExpert/actions/workflows/release.yml/badge.svg)](https://github.com/LogExperts/LogExpert/actions/workflows/release.yml/)

This is a clone from (no longer exists) https://logexpert.codeplex.com/

## Overview

LogExpert is a Windows feature rich tail program (a GUI replacement for the Unix tail command) with support for plugins, highlighting, filtering, bookmarking, columnizing and more.

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

## Download

* Follow the [Link](https://github.com/LogExperts/LogExpert/releases/latest) and download the latest package. Just extract it where you want and execute the application or download the Setup and install it
* Install via chocolatey
  ```choco install logexpert```
* get the Nuget package [Link](https://www.nuget.org/packages/logexpert)
  ```dotnet add package logexpert --version 1.30.0```
* the ColumnizerLib can also be downloaded via nuget [Link](https://www.nuget.org/packages/ColumnizerLib)
  ```dotnet add package ColumnizerLib --version 1.21.0```

Requirements

* <https://dotnet.microsoft.com/en-us/download>
* .NET 10 (<https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-10.0.0-windows-x64-installer>)

## Command line

Open a log at a specific line:

```text
LogExpert.exe "C:\logs\application.log" --line 1234
LogExpert.exe "C:\logs\application.log" -n 1234
```

Line numbers start at 1, as in Go to Line. Supply the option once, with an integer
from 1 through 2147483647 and exactly one log file. Sessions (`.lxj`) and Session
Files (`.lxp`) are not supported with this option. Missing, invalid, repeated, or
out-of-range values produce a command-line error before files are opened.

LogExpert waits for normal loading and Session File restoration, then selects and
reveals the requested line with follow-tail disabled. A number beyond the end
selects the last line; an empty file keeps an empty selection. The same behavior
applies when forwarding to an existing instance: the existing routing preferences
choose the window, and an already-open log is reused. While it loads, the latest
line request wins. Navigation is applied once, so later reloads do not repeat it.

This option uses the loaded log's line numbering, including any preprocessing or
combined rollover view. It does not load only part of a large file or map physical
file lines. Rotation, truncation, or other edits can make an external tool's line
number stale. Commands without `--line` / `-n` keep their existing behavior.

## CI

This is a continous integration build. So always the latest and greates changes. It should be stable but no promises. Can be viewed as Beta.

[CI Download](https://ci.appveyor.com/project/Zarunbal/logexpert)

## How to Build

* Clone / Fork / Download the source code
* Open the Solution (src/LogExpert.sln) with Visual Studio 2026 (e.g. Community Edition)
* Restore Nuget Packages on Solution
* Build
* The output is under bin/(Debug/Release)/

Nuke.build Requirements

* Chocolatey must be installed
* Optional for Setup Inno Script 6.6.1

## Resources / Translations
If you want to contribute translations or add new languages please use the ResXResourceManager Extension to add new or missing
translations for either German / English. With this extension its easy to add a new language as well:
[ResXResourceManager](https://github.com/dotnet/ResXResourceManager/tree/master)

After creating a new language resource please create a new pull request.

## Pull Request

* Use Development branch as target

## FAQ / HELP / Informations / Examples

Please checkout the wiki for FAQ / HELP / Informations / Examples

## High DPI

* dont use AutoScaleMode for single GUI controls like Buttons etc.
* dont use AutoScaleDimensions for single GUI controls like Buttons etc.

<https://github.com/LogExperts/LogExpert/wiki>

## Discord Server

<https://discord.gg/SjxkuckRe9>

### Credits

#### Contributors

This project exists thanks to all the people who contribute.
<a href="https://github.com/LogExperts/LogExpert/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=LogExperts/LogExpert" />
</a>

Made with [contrib.rocks](https://contrib.rocks).
