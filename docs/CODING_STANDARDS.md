# Coding Standards

Rules for judging a change at review time. Formatting and analyzer severities are enforced by `src/.editorconfig`, so they are not repeated here.

## Comments

- **No XML doc comments (`///`) on internal or private members, or on test classes and test methods.** `CS1591` is turned off in `src/.editorconfig` on purpose.
- Add code comments only when the code can't explain itself, and keep them as short as possible.

## Scope of a change

- Every changed line should trace back to the request. Don't "improve" adjacent code, comments or formatting, and don't refactor what isn't broken.
- Match the existing style of the file, even if you would do it differently.
- Remove imports, variables and functions that the change itself made unused. Leave pre-existing dead code alone and mention it instead.

## Design

- Minimum code that solves the problem: no speculative features, configurability, or abstractions for single-use code.
- No error handling for impossible scenarios.
- "Behaviour-preserving" refactors must actually preserve behaviour. Flag any logic change explicitly.

## Repo-specific

- High DPI: `AutoScaleMode` / `AutoScaleDimensions` go on forms only, never on individual controls.
- User-visible strings go in the resx files under `src/LogExpert.Resources` (and the plugin `Resources.resx` files), with the same keys in `de` and `zh-CN`. CI runs `tools/Test-ResxKeyParity.ps1`.
- Bug fixes come with a test that reproduces the bug.
