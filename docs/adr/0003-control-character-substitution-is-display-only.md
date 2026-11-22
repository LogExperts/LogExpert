---
status: accepted
---

# Control Character Substitution is display-only

> Reconstructed on 2026-11-22 from the code (`LogExpert.UI.ControlCharDisplay`, `LogWindow.PaintCell`) and PR #582. The original ADR was lost.

Logs can contain C0 control characters and DEL, which Windows draws as nothing or as a box. We show them as visible glyphs (Caret, C escape, Abbreviation, Unicode Control Pictures or ISO 2047), but only while a cell is painted. `ControlCharRenderer` splits the cell's text into raw and substituted segments inside `LogWindow.PaintCell`. Raw column data is never changed, so search, filter, highlight regexes and columnizer parsing all keep working on the real characters, and a regex for `\x07` still matches a BEL shown as `␇`.

Substitution is off by default. By default it covers every C0 character plus DEL except TAB, LF and CR, which already have a visible effect.

## Consequences

- Copying copies the raw text by default. Copying what is shown is a separate opt-in (`ControlCharSettings.CopyDisplayedForm`, applied by `SubstitutedClipboardBuilder`), and only when substitution is on.
