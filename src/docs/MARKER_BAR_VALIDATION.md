# Marker Bar implementation validation

Issue #27 adds independent overview lanes for highlights, bookmarks, the latest Log Search, and Window Filter hits. The bar remains opt-in by explicit user choice. This report records measured performance and the limits of automated validation.

## Performance measurements

Measured on 2026-09-17 on Windows x64, .NET SDK 10.0.106, Debug, 96 DPI. Both experiments use four regex rules, with the dense matching rule last, 500,000 initial lines, and 2,000 actively appended lines. The measured implementation was committed as `3786f2d2`; the subsequent localization and test-placement changes do not change the scanner or aggregation algorithms.

| Measurement | In-memory marker index | Real file and Log Window |
| --- | ---: | ---: |
| Initial / final lines | 500,000 / 502,000 | 500,000 / 502,000 |
| Initial / final file size | 14,000,000 / 14,056,000 modeled UTF-8 bytes | Final size: 28,048,000 bytes on disk |
| Regex rule count | 4 | 4 |
| Initial discovery duration | 378.719 ms | 929 ms |
| Append processing duration | 4.710 ms | 493 ms, including file monitoring and discovery |
| Managed heap delta after discovery | 8,028,328 bytes | 8,112,792 bytes |
| Managed heap delta during append | 37,736 bytes | Not measured |
| Discovery / append allocations | 84,103,432 / 414,048 bytes | Not measured |
| UI event pumps during discovery | Not applicable | 45 |
| Maximum event-pump duration | Not applicable | 26.8 ms |

The in-memory experiment checks all 502,000 final matches and their counts across 2,000 pixel buckets. Its file sizes model UTF-8 content with CRLF; it does not read disk. The real-file experiment verifies publication of the appended final-line marker and tooltip. Heap deltas use `GC.GetTotalMemory(true)` and allocations use `GC.GetTotalAllocatedBytes(true)`. These are individual-run diagnostics, not process working-set measurements or input-latency guarantees. The explicit tests report timings rather than imposing hardware-dependent CI thresholds.

Reproduce the measurements after building:

```powershell
dotnet test src/LogExpert.Tests/LogExpert.Tests.csproj --no-build --filter 'FullyQualifiedName=LogExpert.Tests.Marker.MarkerPerformanceTests.MarkerIndex_ScanAndAppend_DenseMatchesReportPerformanceAndCorrectness' --logger 'console;verbosity=normal'
dotnet test src/LogExpert.UI.Tests/LogExpert.UI.Tests.csproj --no-build --filter 'FullyQualifiedName=LogExpert.UI.Tests.MarkerWindowTests.DenseFile_ReportsDiscoveryAppendAndUiResponsiveness' --logger 'console;verbosity=normal'
```

## UI coverage

The WinForms fixtures formerly in `LogExpert.Tests` now run in `LogExpert.UI.Tests`, including dialog, control, navigation, menu, and window-service tests. Its setup selects PerMonitorV2 before creating handles, matching the application. Parsing-only fixtures remain in `LogExpert.Tests`; no process-wide DPI setup is added there.

- DPI: discovery-indicator rendering and docked Marker Bar width at 96/144/192 DPI. The width expectations are 28/42/56 device pixels for a 28-logical-pixel bar. Tests deliver WinForms DPI messages to existing handles; physical monitor transitions were not exercised.
- Themes: real Log Windows render inherited highlight colors as black in Classic mode and white in Dark mode; changing the grid foreground to yellow also updates markers. The nonparallel fixture checks that no windows are open before changing application color mode, closes its windows, and restores the original mode. Windows OS theme-setting changes were not exercised.
- Layout and navigation: resizing, hidden/restored docked windows with appended content, simultaneous non-overlapping Time Spread, marker clicks, and stopping follow-tail.
- Data updates: executed search and filter criteria, rule edits, source visibility, restored bookmarks, truncation, timeshift, and independent word-highlight columnizer state.

All 17 marker-related host resource keys have German and Simplified Chinese translations, including settings labels, tooltips and scan errors. Exact key coverage and formatting placeholders were checked without allowing neutral-resource fallback.

## Regression status

Both the baseline and final clean builds passed with 0 errors and 240 warnings. Warning messages were unchanged after accounting for test-project paths. The final full-suite run passed the main project (945 tests, 7 skipped) and the four companion projects (340, 63, 105, and 7 tests). The UI host passed 69 cases before a 60-second inactivity timeout aborted it during `InactiveWindow_KeepsItsOwnSearchWhenAnotherWindowSearches`, the same teardown stall reproduced before these changes. On the final build, the other 297 UI cases passed together, and the inactive-window case passed separately. These separate runs account for 1,758 passing tests and 7 skipped tests across the solution; they do not constitute a successful uninterrupted full-suite run.

The earlier dumps show the UI thread inside native `SetWindowRgn` through DockPanelSuite disposal, and a loading callback waiting on synchronous UI invocation. This does not prove causality. The test passes alone; successful isolated runs do not establish a successful full-suite run. Synthetic DPI thread isolation did not reliably resolve the stall. No docking implementation was changed for this review.

```powershell
./build.ps1 --target Clean Compile
dotnet test src/LogExpert.sln --no-build --logger 'console;verbosity=normal' --blame-hang-timeout 60s --blame-hang-dump-type mini
dotnet test src/LogExpert.UI.Tests/LogExpert.UI.Tests.csproj --no-build --filter 'FullyQualifiedName!~InactiveWindow_KeepsItsOwnSearch'
dotnet test src/LogExpert.UI.Tests/LogExpert.UI.Tests.csproj --no-build --filter 'FullyQualifiedName~InactiveWindow_KeepsItsOwnSearch'
```
