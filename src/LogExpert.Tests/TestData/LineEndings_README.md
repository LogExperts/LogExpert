# Line-ending test files

Hand-crafted fixtures for verifying that the stream readers split lines correctly
regardless of line-ending style. Drag-and-drop or open each in the GUI and confirm
it shows **7 lines**, the 5th of which is **empty**, with the multibyte chars on
line 3 (`ü 世界`) intact.

All files are UTF-8 **without BOM**. The last line has **no trailing terminator**
(exercises the end-of-file tail path). The expected logical content is identical
across every file — only the terminators differ.

| File | Terminator(s) | Notes |
|------|---------------|-------|
| `LineEndings_LF.txt`    | `\n` only        | Unix |
| `LineEndings_CRLF.txt`  | `\r\n` only      | Windows |
| `LineEndings_CR.txt`    | `\r` only        | Classic Mac — a reader that scans for `\n` only shows the whole file as ONE line |
| `LineEndings_Mixed.txt` | `\n`, `\r\n`, `\r` interleaved | Different terminator after each line |

The default `SystemDirect` reader handles all four. The `CR` and `Mixed` files are
the interesting ones: before `Direct` learned to detect the actual terminator per
line, `CR` rendered as a single giant line and `Mixed` drifted the byte position
(visible as wrong seeking when buffers are flushed and reloaded).

Regenerate with `gen-lineendings.ps1` in this folder (writes byte-exact terminators).
