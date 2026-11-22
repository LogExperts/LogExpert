---
status: accepted
---

# The stream reader's position counts the raw bytes consumed

> Reconstructed on 2026-11-22 from PR #756 (issue #639) and the code it introduced in `PositionAwareStreamReaderDirect`. The original ADR was lost.

`LogfileReader` takes a file's size from the reader's byte `Position`, and tail mode treats a file whose size on disk is smaller than that as recreated: it reloads and the view jumps to the top. The readers used to advance `Position` by re-encoding the decoded text. Every invalid byte decodes to U+FFFD, which re-encodes as 3 UTF-8 bytes, so a log containing a binary block reloaded on every append.

We decided that **`Position` always equals the raw bytes consumed, whatever they are**: valid, invalid, truncated sequences or any line ending. The Direct reader finds line terminators in the raw bytes, as code units of the encoding (`\r`/`\n` are one byte, or two/four for UTF-16/UTF-32, and never occur inside a multi-byte character), decodes each line on its own, and advances `Position` by the bytes the line and its terminator really occupy. How invalid bytes are displayed (U+FFFD) did not change.

## Considered Options

- **Keep the `System` and `Legacy` readers alongside Direct:** rejected, and both were removed. They read through `StreamReader`, which never reports the bytes it consumed, so neither could be made exact without becoming a copy of Direct.

## Consequences

- Every read path must keep this invariant. `ReadChar()`, which XML mode uses, still counts re-encoded characters (#757).
- Settings saved with the removed `ReaderType` still load: the unknown member is ignored.
