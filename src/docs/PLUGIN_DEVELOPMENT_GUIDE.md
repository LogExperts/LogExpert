# LogExpert Plugin Development Guide

## Table of Contents

1. [Introduction](#introduction)
2. [Plugin Types](#plugin-types)
3. [Creating a Columnizer Plugin](#creating-a-columnizer-plugin)
4. [Plugin Manifest](#plugin-manifest)
5. [Security & Permissions](#security--permissions)
6. [Testing Your Plugin](#testing-your-plugin)
7. [Distribution](#distribution)
8. [Best Practices](#best-practices)
9. [API Reference](#api-reference)
10. [Troubleshooting](#troubleshooting)

---

## Introduction

LogExpert is an extensible log file viewer that supports various types of plugins. This guide will help you create your own plugins to extend LogExpert's functionality.

### What You Can Build

- **Log Columnizers** - Parse custom log formats into columns
- **Context Menu Plugins** - Add custom actions to context menus
- **Keyword Actions** - React to specific keywords in logs
- **File System Plugins** - Support for custom file sources (e.g., cloud storage)

### Prerequisites

- .NET 10.0 SDK or later
- Visual Studio 2026 or VS Code with C# extension
- Basic C# knowledge
- LogExpert installed for testing

---

## Plugin Types

### 1. Log Columnizers (`ILogLineColumnizer`)

Columnizers parse log lines into columns for tabular display.

**Use cases:**
- Custom log formats
- Proprietary application logs
- Structured log parsing

### 2. Context Menu Plugins (`IContextMenuEntry`)

Add custom items to LogExpert's context menu.

**Use cases:**
- Custom log analysis tools
- Integration with external systems
- Quick actions on selected lines

### 3. Keyword Actions (`IKeywordAction`)

Automatically react to keywords found in logs.

**Use cases:**
- Alert notifications
- Automatic bookmarking
- External tool triggers

### 4. File System Plugins (`IFileSystemPlugin`)

Support for non-local file sources.

**Use cases:**
- Cloud storage (S3, Azure Blob)
- Network shares with custom protocols
- Database log sources

---

## Creating a Columnizer Plugin

### Step 1: Create a Class Library Project

```bash
dotnet new classlib -n MyCustomColumnizer
cd MyCustomColumnizer
```

### Step 2: Add Required References

Edit `MyCustomColumnizer.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\ColumnizerLib\ColumnizerLib.csproj" />
  </ItemGroup>
</Project>
```

### Step 3: Implement the Interface

Create `MyColumnizer.cs`:

```csharp
using ColumnizerLib;
using LogExpert;

namespace MyCustomColumnizer;

public class MyColumnizer : ILogLineColumnizer
{
    public string GetName()
    {
        return "My Custom Columnizer";
    }
    
    public string GetDescription()
    {
        return "Parses custom application logs";
    }
    
    public int GetColumnCount()
    {
        return 3; // Number of columns
    }
    
    public string[] GetColumnNames()
    {
        return new[] { "Timestamp", "Level", "Message" };
    }
    
    public IColumnizedLogLine SplitLine(ILogLineColumnizerCallback callback, ILogLine line)
    {
        // Parse the log line
        var parts = line.FullLine.Split('|');
        
        var columns = new Column[3];
        columns[0] = new Column { FullValue = parts.Length > 0 ? parts[0].Trim() : "" };
        columns[1] = new Column { FullValue = parts.Length > 1 ? parts[1].Trim() : "" };
        columns[2] = new Column { FullValue = parts.Length > 2 ? parts[2].Trim() : "" };
        
        return new ColumnizedLogLine
        {
            LogLine = line,
            ColumnValues = columns
        };
    }
    
    public bool IsTimeshiftImplemented()
    {
        return false; // Set to true if you implement timestamp parsing
    }
    
    public void SetTimeOffset(int msecOffset)
    {
        // Implement if IsTimeshiftImplemented() returns true
    }
    
    public int GetTimeOffset()
    {
        return 0;
    }
    
    public DateTime GetTimestamp(ILogLineColumnizerCallback callback, ILogLine line)
    {
        // Implement if IsTimeshiftImplemented() returns true
        return DateTime.MinValue;
    }
    
    public void PushValue(ILogLineColumnizerCallback callback, int column, string value, string oldValue)
    {
        // Implement if you want to support editing
    }
}
```

### Step 4: Build the Plugin

```bash
dotnet build -c Release
```

Your plugin DLL will be in `bin\Release\net8.0\MyCustomColumnizer.dll`.

---

## Plugin Manifest

Create a manifest file to provide metadata about your plugin.

### Create `MyCustomColumnizer.manifest.json`

```json
{
  "name": "MyCustomColumnizer",
  "version": "1.0.0",
  "author": "Your Name",
  "description": "Parses custom application log format with pipe delimiters",
  "apiVersion": "2.0",
  "main": "MyCustomColumnizer.dll",
  "url": "https://github.com/yourusername/mycustomcolumnizer",
  "license": "MIT",
  "requires": {
    "logExpert": ">=1.10.0",
    "dotnet": ">=10.0.0"
  },
  "permissions": [
    "filesystem:read"
  ]
}
```

### Manifest Fields

| Field | Required | Description |
|-------|----------|-------------|
| `name` | Yes | Plugin name (must match DLL without extension) |
| `version` | Yes | Semantic version (e.g., "1.0.0") |
| `author` | Yes | Your name or organization |
| `description` | Yes | Brief description of plugin functionality |
| `apiVersion` | Yes | LogExpert API version (current: "2.0") |
| `main` | Yes | Main DLL filename |
| `url` | No | Plugin website or repository |
| `license` | No | License identifier (e.g., "MIT", "Apache-2.0") |
| `requires` | No | Version requirements |
| `permissions` | No | Required permissions |
| `dependencies` | No | External dependencies |

---

## Security & Permissions

### Available Permissions

- `filesystem:read` - Read files from disk
- `filesystem:write` - Write files to disk
- `network:connect` - Make network connections
- `config:read` - Read configuration files
- `config:write` - Write configuration files
- `registry:read` - Read Windows registry

### Best Practices

1. **Request minimal permissions** - Only ask for what you need
2. **Document permissions** - Explain why each permission is needed
3. **Validate inputs** - Always validate log data
4. **Handle errors gracefully** - Don't crash the host application
5. **Avoid side effects** - Don't modify global state unexpectedly

### Example with Permissions

```json
{
  "name": "NetworkLogColumnizer",
  "permissions": [
    "filesystem:read",
    "network:connect"
  ]
}
```

---

## Testing Your Plugin

### Manual Testing

1. **Build your plugin:**
   ```bash
   dotnet build -c Release
   ```

2. **Copy files to LogExpert plugins folder:**
   ```bash
   copy bin\Release\net8.0\MyCustomColumnizer.dll "C:\Program Files\LogExpert\plugins\"
   copy MyCustomColumnizer.manifest.json "C:\Program Files\LogExpert\plugins\"
   ```

3. **Trust the plugin:**
   - Open LogExpert
   - Go to **Settings > Plugin Trust Management**
   - Click **"Add Plugin..."**
   - Select your DLL file
   - Confirm trust

4. **Test the plugin:**
   - Open a log file
   - Go to **Settings > Columnizer**
   - Select your columnizer
   - Verify it parses logs correctly

### Automated Testing

Create unit tests for your plugin:

```csharp
using NUnit.Framework;
using MyCustomColumnizer;

[TestFixture]
public class MyColumnizerTests
{
    [Test]
    public void SplitLine_ParsesCorrectly()
    {
        // Arrange
        var columnizer = new MyColumnizer();
        var line = new LogLine("2024-01-15 10:30:00 | INFO | Application started", 0);
        var callback = new MockCallback();
        
        // Act
        var result = columnizer.SplitLine(callback, line);
        
        // Assert
        Assert.That(result.ColumnValues.Length, Is.EqualTo(3));
        Assert.That(result.ColumnValues[0].FullValue, Is.EqualTo("2024-01-15 10:30:00"));
        Assert.That(result.ColumnValues[1].FullValue, Is.EqualTo("INFO"));
        Assert.That(result.ColumnValues[2].FullValue, Is.EqualTo("Application started"));
    }
}
```

---

## Distribution

### Option 1: GitHub Release

1. Create a GitHub repository
2. Create a release
3. Attach plugin DLL and manifest
4. Users download and install manually

### Option 2: Direct Distribution

1. Create a ZIP file with:
   - Plugin DLL
   - Manifest JSON
   - README with installation instructions
2. Distribute via your website or email

### Installation Instructions Template

```markdown
# MyCustomColumnizer Installation

## Requirements
- LogExpert 1.10.0 or later
- .NET 8.0 runtime

## Installation

1. Download `MyCustomColumnizer.zip`
2. Extract to a temporary folder
3. Copy files to LogExpert plugins folder:
   - `MyCustomColumnizer.dll`
   - `MyCustomColumnizer.manifest.json`
4. Restart LogExpert
5. Trust the plugin:
   - Settings > Plugin Trust Management
   - Add Plugin... > Select `MyCustomColumnizer.dll`
6. Use the plugin:
   - Settings > Columnizer > Select "My Custom Columnizer"
```

---

## Best Practices

### Code Quality

1. **Error handling:**
   ```csharp
   public IColumnizedLogLine SplitLine(ILogLineColumnizerCallback callback, ILogLine line)
   {
       try
       {
           // Your parsing logic
       }
       catch (Exception ex)
       {
           // Return safe default on error
           return CreateDefaultColumns(line);
       }
   }
   ```

2. **Performance:**
   - Avoid regex if simple parsing works
   - Cache compiled regexes if you use them
   - Don't allocate unnecessarily

3. **Null safety:**
   ```csharp
   public IColumnizedLogLine SplitLine(ILogLineColumnizerCallback callback, ILogLine line)
   {
       if (line?.FullLine == null)
       {
           return CreateEmptyColumns(line);
       }
       // Process line...
   }
   ```

### Documentation

1. **Clear README:**
   - What the plugin does
   - Installation instructions
   - Usage examples
   - Known limitations

2. **Code comments:**
   - Document complex parsing logic
   - Explain regex patterns
   - Note any assumptions

3. **Changelog:**
   - Keep a changelog
   - Use semantic versioning
   - Document breaking changes

---

## API Reference

### Core Interfaces

#### ILogLineColumnizer

Main interface for columnizer plugins.

**Methods:**
- `string GetName()` - Plugin name
- `string GetDescription()` - Plugin description
- `int GetColumnCount()` - Number of columns
- `string[] GetColumnNames()` - Column names
- `IColumnizedLogLine SplitLine(...)` - Parse log line
- `bool IsTimeshiftImplemented()` - Supports timestamps?
- `DateTime GetTimestamp(...)` - Extract timestamp
- ... (see ColumnizerLib for complete reference)

#### Optional marker snapshots (`ICloneable`)

A columnizer implementing `ILogLineMemoryColumnizer` can also implement
`System.ICloneable` to preserve its current parsing state for the marker bar's
background scan. This is optional; the columnizer interface has no new required
members, and existing plugins do not need recompilation for this feature.

`Clone()` must return an independent, initialized `ILogLineMemoryColumnizer` with
its current configuration and detected layout. Do not share mutable parsing state
with the original instance. The marker bar calls `Clone()` on the UI thread, so
keep it quick and avoid file I/O. It parses using the copy on a worker thread and
does not call `Selected()` on that copy.

Without a compatible clone, the marker bar constructs a fresh instance using the
plugin's public parameterless constructor, calls optional `LoadConfig()` and
`Selected()` on the worker, and applies the current time offset when supported.
Runtime state that cannot be reconstructed this way requires the optional clone.

See [the marker columnizer capture](../LogExpert.UI/Controls/LogWindow/LogWindow.MarkerBar.cs)
and [the fallback factory](../LogExpert.Core/Classes/Columnizer/ColumnizerPicker.cs).

#### ILogLine

Represents a single log line.

**Properties:**
- `string FullLine` - Complete line text
- `int LineNumber` - Zero-based line number

#### IColumn

Represents a column value.

**Properties:**
- `string FullValue` - Complete value
- `string DisplayValue` - Truncated display value

---

## Troubleshooting

### Plugin Not Appearing

**Problem:** Plugin doesn't show up in LogExpert

**Solutions:**
1. Check DLL is in correct folder
2. Verify manifest JSON is valid
3. Check LogExpert logs for errors
4. Ensure DLL targets correct .NET version

### Plugin Not Trusted

**Problem:** Plugin is blocked by security

**Solution:**
1. Open Settings > Plugin Trust Management
2. Click "Add Plugin..."
3. Select your DLL
4. Confirm trust

### Parsing Issues

**Problem:** Columns not parsing correctly

**Debug steps:**
1. Add logging to your SplitLine method
2. Test with various log line formats
3. Check for null/empty lines
4. Verify column count matches GetColumnCount()

### Performance Issues

**Problem:** LogExpert slow with your plugin

**Optimizations:**
1. Profile your SplitLine method
2. Avoid regex if possible
3. Cache compiled patterns
4. Don't allocate in hot paths

---

## Example Plugins

### Simple CSV Parser

```csharp
public class CsvColumnizer : ILogLineColumnizer
{
    public IColumnizedLogLine SplitLine(ILogLineColumnizerCallback callback, ILogLine line)
    {
        var parts = line.FullLine.Split(',');
        var columns = parts.Select(p => new Column 
        { 
            FullValue = p.Trim() 
        }).ToArray();
        
        return new ColumnizedLogLine
        {
            LogLine = line,
            ColumnValues = columns
        };
    }
    
    // ... other interface methods
}
```

### Regex-Based Parser

```csharp
public class RegexColumnizer : ILogLineColumnizer
{
    private static readonly Regex _pattern = new Regex(
        @"^(\d{4}-\d{2}-\d{2})\s+(\w+)\s+(.+)$",
        RegexOptions.Compiled);
    
    public IColumnizedLogLine SplitLine(ILogLineColumnizerCallback callback, ILogLine line)
    {
        var match = _pattern.Match(line.FullLine);
        if (!match.Success)
        {
            return CreateDefaultColumns(line);
        }
        
        var columns = new Column[3];
        columns[0] = new Column { FullValue = match.Groups[1].Value };
        columns[1] = new Column { FullValue = match.Groups[2].Value };
        columns[2] = new Column { FullValue = match.Groups[3].Value };
        
        return new ColumnizedLogLine
        {
            LogLine = line,
            ColumnValues = columns
        };
    }
}
```

---

## Support

### Getting Help

- **GitHub Issues:** [LogExperts/LogExpert/issues](https://github.com/LogExperts/LogExpert/issues)
- **Documentation:** Check LogExpert wiki
- **Example Plugins:** See built-in columnizers source code

### Contributing

Contributions welcome! If you create a useful plugin, consider:
- Submitting it to LogExpert repository
- Sharing on plugin directory (coming soon)
- Writing a blog post about it

---

**Happy Plugin Development!**

*This guide is for LogExpert 1.11.0 and later.*
