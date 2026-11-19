# Agent Guidelines

Behavioural rules for AI agents working in this repo: how to plan, clarify, delegate and verify. Project facts (build, tests, gotchas) live in the root `CLAUDE.md`.

## Karpathy Guidelines

Behavioral guidelines to reduce common LLM coding mistakes.

**Tradeoff:** These guidelines bias toward caution over speed. For trivial tasks, use judgment.

### 0. Fast Path for Trivial Tasks

**For obvious, low-risk, reversible, text only ( no source code ), single-file changes, execute directly with minimal ceremony.**

- Do not create a plan, spawn or delegate to agents, run quorum review, inspect unrelated files, perform broad searches, or run Git archaeology.
- Read only the target and, when genuinely necessary, one directly relevant instruction or nearby example.
- Do not ask questions when the request and local convention determine the result; make the obvious reversible assumption.
- Use the minimum tool calls: inspect, edit, and verify once. Keep progress narration to one short update at most.
- If the preferred editing tool fails once because of an environment or tooling problem, use a safe direct fallback. Do not repeatedly debug the tool unless no safe fallback exists.
- Verification should be proportional to the change. For a simple text edit, rereading the changed content is sufficient.

This fast path overrides the planning, clarification, delegation, and quorum-review requirements below for trivial tasks. Higher-priority system, developer, safety, and explicitly invoked skill instructions still apply.

### 1. Clarify Before Executing

**Resolve ambiguity up front — before spawning agents or starting any autonomous work.**

Do this in order, then stop and ask before proceeding:

1. **Understand the request.** Read what was asked closely and state your assumptions explicitly.
2. **Inspect the codebase.** Check the relevant code, conventions, and existing patterns. Note anything that's unclear or that could be interpreted in more than one way.
3. **Ask.** Raise every consequential question now, batched together — don't pick an interpretation silently, and don't defer the question into the work itself.

Throughout:

- If multiple interpretations exist, present them — don't choose one silently.
- If a simpler approach exists, say so. Push back when warranted.
- Don't invent APIs. Confirm a function, flag, or endpoint exists — and check its signature — before calling it. If you can't verify it, say so rather than guessing.

Only once these questions are resolved do you move to execution (§4).

### 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

**Minimal comments**:

- avoid adding code comments unless necessary (i.e. only if the code itself is not self explanatory). When necessary, code comments must be as brief as possible to ensure readability and avoid overbloating.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

### 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:

- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you spot problems outside your task — dead code, bugs — mention them; don't fix or delete them unless asked.

When your changes create orphans:

- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: every changed line should trace directly to the user's request.

### 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Begin only once §1's questions are resolved. Then turn the task into a verifiable goal with an observable check. A passing test is the strongest check; a clean typecheck, running it and inspecting output, or diffing against expected output also count.

- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"
- "Update the config" → "Apply it, start the service, and confirm the setting takes effect"

For multi-step tasks, state a brief plan:

```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

When no obvious check exists (docs, exploration, some one-off scripts), ask how to verify success before starting. Don't report success you haven't actually observed.

Strong, agreed-upon criteria — settled in §1 — let you loop independently. Weak criteria ("make it work") force constant clarification, which is why §1 comes first.

### 5. Delegate Grunt Work Down a Tier

**Spend the capable model on judgment; push mechanical, low-context work to a cheaper model.**

Act as an orchestrator. Fan out grunt work — reading files, gathering context, simple searches, minor mechanical edits, other simple tool calls — to cheaper agents, then review their findings and any code changes yourself before acting on them. Keep the important and dangerous work — architecture, ambiguous decisions, risky or hard-to-reverse edits, final review — on the more capable model.

When delegation is available, use a less capable, cheaper model for genuinely simple, mechanical, low-risk work. Keep tasks requiring substantial context or judgment on the primary, more capable model.

Match the model to the task: pick the cheapest tier that can do the job well, and skip an intermediate tier when the work is simple enough for a lower one.

Guidelines:

- Only delegate work that is genuinely low-context and low-risk. If doing it well requires the capable model's judgment, keep it.
- Give each agent a self-contained task with clear success criteria (per §4) so you can verify its output without redoing the work.
- Never merge a delegated finding or edit unblocked — review it first. The cheaper model did the legwork; you own the decision.

### 6. Quorum Review for Big Changes

**For bigger changes, three agents check the work and two of three must agree before it counts as a success.**

This applies to any change that is large, or that spans multiple files. For trivial, single-file edits, skip it and use §4's normal verification.

- Spawn **three independent agents** to review the completed change, each judging against the success criteria settled in §1/§4.
- **Vary each agent's prompt slightly so they are not identical.** Give them the same context and criteria, but frame each reviewer differently — a distinct persona or emphasis (e.g. one as a correctness-focused engineer, one probing edge cases, one checking that it actually satisfies the request). Don't hand all three the same "You are a systems engineer…" opening.
- The change is a success only when **at least two of the three agree** it is correct and complete.
- If the vote fails (two or more say it's wrong or incomplete), treat it as not done: address their objections, then re-run the quorum.
- Keep their judgments independent so the vote is meaningful, and read their reasoning yourself before accepting the verdict.

### 7. Reporting Results

- Keep straightforward findings and small fixes in chat.
- If a review has four or more substantive findings, or the detailed response would exceed roughly 25 lines, write the full results to a clearly named Markdown file in the repository.
- In chat, provide only a short summary and a link to that report.
- Do not create a report file for a simple bug, small change, or brief explanation.

## Other Working Rules

### Documentation

Docs must be code-grounded and reconciled against the actual source and live config, then reader-tested before delivery. Don't document from assumption.

### Refactoring

For "behavior-preserving" refactors, explicitly confirm equivalence and flag any unintended logic changes before applying.
