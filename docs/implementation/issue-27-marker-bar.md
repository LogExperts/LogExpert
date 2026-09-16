# Issue 27: log marker bar

Issue: https://github.com/LogExperts/LogExpert/issues/27

Enable **Settings → Marker bar → Show marker bar**. The four fixed lanes, from left to right, represent highlights, bookmarks, Log Search hits, and Window Filter hits. Each source has its own saved visibility setting. The bar is initially hidden. Right-click it and choose **Clear search** to remove that window's search markers; an unchanged F3 search leaves them cleared.

Markers use logical lines, including for files without timestamps. A click selects and reveals the matching line nearest the pixel bucket's logical midpoint, with lower-line tie breaking, and stops follow-tail. Tooltips report the category, one-based line range, and matching-line count. Time Spread retains its separate layout column and behavior.

## Implementation and review evidence

- `MarkerCriteria` snapshots visual rules/search criteria and performs pure matching. Whole-line rules inspect raw text and displayed columns; word rules inspect displayed columns. Temporary search highlights and trigger-only rules are excluded. No marker code calls highlight triggers.
- `MarkerIndex` scans on a worker, pins small reader batches, retains immutable match chunks, and rereads only the former final line plus appended content. Reset/close cancels scans; obsolete generations cannot replace current results. Regex failures become reported outcomes.
- `MarkerBucket` aggregates every matching line. Counts exclude duplicate lines, colors follow group priority, endpoints reach the top/bottom, and resizing consumes cached matches.
- `LogWindow.MarkerBar` batches updates with a 250 ms UI timer and aggregates pixels in the background. It consumes bookmark snapshots and actual filter hit lists, including partial filter results. A short lifecycle lock couples frame snapshots to their generation and protects publication against rollover/reload and criteria changes.
- Column discovery uses an independent parser. CSV, Regex, and Log4j implement `ICloneable` to preserve their current configuration/runtime state; other columnizers use the existing factory and initialization callback. Custom stateful columnizers can implement `ICloneable` to preserve state beyond saved configuration. Timestamp offsets are captured and changes invalidate markers.
- The standards review checked repository instructions, `.editorconfig`, neighboring code, resource localization, Core/UI separation, disposal, and the absence of per-control auto-scaling. An unrelated generated resource change was reverted.
- The issue review identified displayed-column matching, parser isolation, atomic frame generation, and timeshift invalidation defects. All four were corrected and re-reviewed; regression tests cover the matching and parser behavior.

## Standards

No remaining substantive Standards findings. The unrelated PluginTrust resource change was restored. The final review checked CSV, Regex, and Log4j snapshots, marker integration, the test project reference, and public configuration-based snapshot tests. No actionable smell-baseline findings remained. A final test-harness finding was corrected by registering the temporary UI exception handler after successful setup and detaching it in a cleanup finally block. Changes follow existing Core/UI boundaries and repository conventions.

## Spec

No blocking Spec findings or scope creep remained after correcting the four review defects. Independent columnizers preserve configured parsing state; generation checks protect publication; matching includes displayed columns; timestamp shifts invalidate highlights. Setup failures use existing logging and status reporting.

One non-blocking observation remains: bookmark/filter data changes use the nominal 250 ms batched refresh. A removed marker can remain clickable until that refresh because navigation validates its file/criteria generation, not every data revision.

Final review totals: Standards 0 remaining findings; Spec 0 blocking findings and 1 minor batching observation. Both final reviews were static; runtime evidence is reported separately below.

## Validation

- Required clean Nuke build passed with zero errors. The clean build reported 252 warnings; subsequent test-await cleanup removed the new CA2007 warnings. Remaining marker-specific warnings are the intentional exception boundaries (CA1031) and a CSV test-data literal (CA1303); no analyzer settings were changed or suppressed.
- Required Nuke full-suite run was attempted. Four projects completed successfully: Persister/Core 128, PluginRegistry 340, RegexColumnizer 63, and ColumnizerLib 7 tests. The main test host was interrupted after an unresponsive UI run; the marker fixture correction described below prevents modal exception dialogs in its tests.
- Final main-suite rerun excluding `ClipboardHelperTests`: **1,196 passed, 7 skipped, 0 failed** (4.12 minutes). Clipboard tests had already crashed the original Development baseline with `ClipboardLock` unable to open the clipboard; they remain an unverified environmental dependency for this change. Across the five completed project runs, **1,734 tests passed**.
- An earlier main-suite rerun had one failure in `Startup_SavedPositionAndTail_ExplicitTargetWins(True)` (expected line 2, observed 3). Both focused cases passed on the feature branch and original Development; all 14 baseline navigation-fixture cases passed, and the final main-suite rerun passed the case. No navigation logic was changed to address that transient failure.
- Marker coverage comprises **23 Core tests and 25 UI/settings/columnizer tests**, plus two passing explicit performance experiments. The final focused rerun also covers the exception-handler cleanup added after the main run.

Validation logs are local TEMP artifacts: `logexpert-27-clean-compile.log`, `logexpert-27-full-tests.log`, `logexpert-27-main-final.log`, `logexpert-27-ui-complete.log`, `logexpert-27-index-performance-final.log`, and `logexpert-27-real-performance-isolated.log`.

Reproduction commands (Windows, .NET 10):

```powershell
./build.ps1 --target Clean Compile
./build.ps1 --target Test
# Baseline clipboard fixture requires a usable desktop clipboard
dotnet test src/LogExpert.Tests/LogExpert.Tests.csproj --no-build --filter 'FullyQualifiedName!~ClipboardHelperTests' --blame-hang-timeout 2m --blame-hang-dump-type none

dotnet test src/LogExpert.Persister.Tests/LogExpert.Persister.Tests.csproj --no-build --filter FullyQualifiedName~LogExpert.Persister.Tests.Marker
dotnet test src/LogExpert.Tests/LogExpert.Tests.csproj --no-build --filter 'FullyQualifiedName~LogExpert.Tests.UI.Marker|FullyQualifiedName~PreferencesMarkerBarTests|FullyQualifiedName~SettingsDialogMarkerBarTests'

# Explicit performance experiments
dotnet test src/LogExpert.Persister.Tests/LogExpert.Persister.Tests.csproj --no-build --filter FullyQualifiedName~MarkerIndex_ScanAndAppend_DenseMatchesReportPerformanceAndCorrectness --logger 'console;verbosity=detailed'
dotnet test src/LogExpert.Tests/LogExpert.Tests.csproj --no-build --filter FullyQualifiedName~DenseFile_ReportsDiscoveryAppendAndUiResponsiveness --logger 'console;verbosity=detailed'
```

The UI fixtures use real files and a WinForms message loop. They cover navigation/tail stopping, all four lanes, filter spread exclusion, restoration/truncation, inactive windows with independent searches, resizing, Time Spread coexistence, parser isolation, and timestamp shifts. Rendering tests check light/dark backgrounds and 100%, 150%, and 200% scaled geometry. They do not substitute for testing native theme changes and dragging the application between physical monitors with different DPI settings.

## Performance measurements

Measured on Windows 11 (10.0.22631), .NET SDK 10.0.106, Debug, 14 logical processors; native test-window DPI was 96. These are single local runs, with other validation work running concurrently.

| Experiment | Initial discovery | Append | Retained managed-memory delta |
| --- | ---: | ---: | ---: |
| Real watched file, 500,000 lines / 28,000,000 bytes, four regex rules, every line matching | 3,939 ms | 2,000 lines / 48,000 bytes: 737 ms including watcher and UI refresh | 6,098,936 bytes after discovery |
| In-memory reader, 500,000 lines / modeled 14,000,000 UTF-8 bytes, same four-rule shape | 1,870.5 ms | 2,000 lines: 9.43 ms | 6,061,952 bytes after discovery; 28,472 bytes after append |

The real file finished at 502,000 lines and 28,048,000 bytes. During discovery, the test serviced 206 WinForms message-loop pumps; the longest `Application.DoEvents` call took 56.1 ms. This measures UI message processing, not end-to-end input latency or every frame interval. The in-memory test verified all 502,000 matches and all bucket counts (2,000 pixel buckets); cumulative allocations were 266,137,160 bytes for discovery and 1,124,704 bytes for append. Retained memory is measured with forced GC and includes runtime/reader caching, not only the marker index.

A prior benchmark attempt entered a WinForms exception dialog in DockPanel document-icon painting (`Icon.Handle` / `VS2013DockPaneStrip.DrawTab_Document`), while the marker worker was idle. The marker fixture now disables shell document icons and captures UI exceptions as test failures instead of opening modal dialogs. Production icon behavior is unchanged; the measured control remains the real marker bar in a real Log Window.
