# Commit message cleanup

Enable the hook once per clone, from the repository root:

```sh
git config --local core.hooksPath .githooks
```

On Linux or macOS, also run `chmod +x .githooks/commit-msg` if needed.
Git for Windows includes the shell and utilities used by this hook.

Before a commit is created, `commit-msg` removes:

- All `Co-authored-by:` lines, regardless of capitalization or author.
- Entire lines containing recognized AI conversation/session URLs for ChatGPT,
  Codex, Claude, Cursor, Gemini, Microsoft Copilot, or GitHub Copilot.

The URL patterns live in `commit-msg`; add patterns there for other providers
or URL formats. Ordinary links, including GitHub issues and pull requests, stay.
If a session URL shares a line with other text, that entire line is removed.

This applies to future local commits, including amendments; it does not rewrite
existing history. `git commit --no-verify` skips this hook. Hooks are not installed
automatically when someone clones the repository.
