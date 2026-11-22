# Issue tracker: GitHub (read-only for skills)

Issues and PRDs for this repo live as GitHub issues on `LogExperts/LogExpert`.

**Write policy: read-only.** Skills read issues for analysis; **only humans post**. Never create, comment on, label, edit, or close an issue or PR — instead, produce the content as a draft and hand it to the user to post.

## Conventions (reading)

Use the `gh` CLI when available. Check that `gh` is actually installed before building a plan around it (see the Environment section of the root `CLAUDE.md`); for public reads, the REST API works unauthenticated as a fallback (`https://api.github.com/repos/LogExperts/LogExpert/issues/...` via `Invoke-RestMethod`).

- **Read an issue**: `gh issue view <number> --comments`, filtering comments by `jq` and also fetching labels.
- **List issues**: `gh issue list --state open --json number,title,body,labels,comments --jq '[.[] | {number, title, body, labels: [.labels[].name], comments: [.comments[].body]}]'` with appropriate `--label` and `--state` filters.
- **Read a PR**: `gh pr view <number> --comments` and `gh pr diff <number>` for the diff.

GitHub shares one number space across issues and PRs, so a bare `#42` may be either — resolve with `gh pr view 42` and fall back to `gh issue view 42`.

## Pull requests as a triage surface

**PRs as a request surface: no.** Triage looks only at issues; PRs are handled by humans as usual.

## When a skill says "publish to the issue tracker"

Do **not** run `gh issue create`. Write the issue as a markdown draft — title, body, and suggested labels — either inline in the conversation or as a file under `.scratch/`, and ask the user to post it.

## When a skill says "fetch the relevant ticket"

Run `gh issue view <number> --comments` (reading is allowed).

## When a skill says "apply a label" / "comment" / "close"

State which label/comment/close action the skill calls for and let the user do it. Skills never mutate the tracker.

## Wayfinding operations

`/wayfinder` normally creates and claims issues, which the read-only policy forbids. When wayfinding this repo, keep the map and tickets as local markdown under `.scratch/wayfinder/` instead of GitHub issues.
