# SysoutPipe is fire-and-forget; temp file is not cleaned up

When `ToolLaunchService` starts an External Tool with Sysout Capture, it creates a `SysoutPipe` and does not return it to the caller. The pipe's background reader thread closes the writer when the process stdout stream ends. The temp file created by `SysoutPipe` is never deleted.

We chose not to return `IDisposable` from `ToolLaunchService.Launch` because there is no natural owner: the pipe outlives the launch call, the Log Window that displays it does not know about the pipe, and the process exit event is already wired internally. Returning the pipe would hand the caller an `IDisposable` with no clear disposal point.

The temp-file leak is a known consequence. A future fix requires a lifetime-tracking mechanism (e.g. pipe registry keyed to the Log Window, cleaned up on tab close) — that scope belongs in a separate change.

## Considered Options

- **Return `SysoutPipe` in `ToolLaunchResult`** — rejected: no caller can determine when to dispose it without tracking the associated Log Window lifetime.
- **Delete temp file on process exit** — rejected: the Log Window may still be reading the file when the process exits.
