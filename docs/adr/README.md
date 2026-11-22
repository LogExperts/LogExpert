# Architecture Decision Records

Format and numbering: `.claude/skills/grill-with-docs/ADR-FORMAT.md`. Take the next free number after the highest one here.

| ADR | Decision |
|---|---|
| [0001](0001-audio-alert-cooldown-is-global.md) | Audio alert cooldown is global, and the active cooldown wins |
| [0002](0002-sysout-pipe-fire-and-forget.md) | SysoutPipe is fire-and-forget; temp file is not cleaned up |
| [0003](0003-control-character-substitution-is-display-only.md) | Control Character Substitution is display-only |
| [0004](0004-columnizer-mask-type-glob-default-legacy-regex.md) | Columnizer masks: Glob for new entries, Regex for existing ones |
| [0005](0005-columnizer-selection-priority-enum.md) | Columnizer Selection Priority replaces the MaskPrio bool |
| [0010](0010-stream-position-counts-raw-bytes.md) | The stream reader's position counts the raw bytes consumed |

## History

Until 2026-11-22, `docs/` was gitignored, so ADRs were written but never committed, and most were lost. 0001, 0003, 0004, 0005 and 0010 were reconstructed from the code, tests and PRs that cite them. Each says so at the top.

- The SysoutPipe ADR was originally numbered 0001, which collided with the audio cooldown ADR that `CONTEXT.md` and the tests cite as 0001. It is now 0002.
- 0006–0009 are not cited anywhere, and their content is unknown. The numbers are left unused rather than reassigned, so an old reference can't point to the wrong decision.
