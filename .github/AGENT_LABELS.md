# Issue label convention

This is the contract the `agent-relay` workflow runs on. Keep it in sync
with `.github/workflows/agent-relay.yml` if you change it.

## Role labels — whose turn it is

Exactly one of these should be present on an issue that's currently
assigned to an agent. Adding one is what triggers that agent's workflow
run.

- `agent:solutions-architect`
- `agent:systems-engineer`
- `agent:software-engineer`
- `agent:test-engineer`
- `agent:cicd`

Never leave two `agent:*` labels on the same issue at once. If it's
unclear who should act next, that's itself a question for
`agent:solutions-architect`.

## Status labels — modifiers, not triggers

- `status:in-progress` — an agent run is currently active on this issue
- `status:blocked` — paired with `agent:solutions-architect`; marks this
  as an escalation rather than a fresh assignment
- `status:ready-for-test` — implementation complete, awaiting the Test
  Engineer
- `status:ready-for-rtvm-update` — a test passed; paired with
  `agent:systems-engineer`. Signals the fast path: update the RTVM
  status for the relevant requirement, then pass straight to
  `agent:cicd` — this is not a new requirement to define.
- `status:ready-for-commit` — RTVM is current and tests passed,
  awaiting CI/CD
- `status:verified` — the linked RTVM item is closed
- `status:cancelled` — a test procedure or requirement changed
  mid-test; the in-flight test iteration is void and will restart once
  a new build is ready
- `status:needs-human` — an automated escalation path has been
  exhausted (e.g. five consecutive fail/rebuild/retest cycles on the
  same requirement). The relay stops here on purpose. A human reviews
  the thread and either resolves it directly or manually relabels to
  resume.
- `status:waiting-on-lock` — this issue's agent backed off after
  failing to acquire a file lock (see `docs/LOCKING.md`). A scheduled
  sweep retries it periodically; no action needed unless it's stuck
  for an unusually long time.

## Type labels

- `type:requirement` — traces to a specific RTVM line item
- `type:blocker` — a question raised by an agent, not a client-facing ask
- `type:bug`

## Title convention

Issues that trace to a requirement start with the RTVM ID:

```
[RTVM-014] Short description of the requirement
```

This makes the RTVM ID searchable across issues, commits, and PRs without
needing a label per ID.

## Escalation ladder

No role is a transparent pass-through for a question it can't answer.
The ladder: `cicd` → `test-engineer` → `software-engineer` →
`systems-engineer` → `solutions-architect` → user. When a role can't
resolve something itself:

1. **Try to resolve it first.** Don't escalate reflexively — the next
   rung up isn't always more qualified, just next in line.
2. **If you can't, escalate to the next rung up — in your own words.**
   Summarize, rephrase, or reference ("see Systems Engineer's
   questions 1–3 above") rather than forwarding verbatim. If you're
   relaying a question that already climbed from further down the
   ladder, say so, so the next rung knows this didn't originate with
   you.
3. **When an answer comes back down to you, relay it to whoever
   escalated to you** — don't just resolve your own concern and move
   on. The role that originally asked should get an answer via the
   same chain it went up, not silence.

One deliberate exception skips the ladder, landing on
`agent:solutions-architect` directly: Test Engineer's
5-consecutive-failure escalation. Every rung already had its shot at
this exact problem, repeatedly, and failed; climbing it again would
repeat a demonstrated failure rather than add fresh consideration.

Every other escalation — including Solutions Architect asking the
user — climbs one rung at a time.

## Notify vs. hand off

These are different actions and shouldn't be conflated:

- **Notify** — a comment addressed to a role by name, for their
  awareness. No relabel. Use this when the next *action* isn't theirs
  but they need to know something changed (e.g. Solutions Architect
  telling Systems Engineer about a scope refinement nobody asked for;
  Systems Engineer telling Software Engineer an RTVM item changed).
- **Hand off** — a comment plus a relabel, because the next action
  genuinely is theirs.

When a rule says "notify X, then notify Y" and both are real actions
someone has to take (not just awareness), treat it as two sequential
handoffs — X acts and relabels to Y — rather than trying to address two
roles' turns at once. See `status:ready-for-rtvm-update` above for the
concrete example.

## Persisting your work

Every job starts from a fresh `git checkout` and its container is
destroyed the moment the job ends — nothing local carries over to the
next run, for any role. Writing a file with Edit/Write isn't enough by
itself; if you don't commit and push it before you finish, it's gone,
not just uncommitted. This applies equally to documents (RTVM, SDD,
PROJECT_DEFINITION) and to your own `MEMORY.md` — a memory update that
happens after your last commit in a run is lost exactly the same way
a code change would be. Make committing and pushing everything you
touched the last thing you do, every run, regardless of what else
you've already committed earlier in that same run.

## Document locations

Every role's file references these; keep the paths consistent across
projects built from this template:

- `docs/PROJECT_DEFINITION.md` — Solutions Architect's scope
  definition: business analysis, stakeholder needs, MVP definition
- `docs/RTVM.md` — Systems Engineer's requirements traceability and
  verification matrix (plain markdown table: ID, category, requirement,
  verification method, test procedure reference, status)
- `docs/SDD.md` — Systems Engineer's software design document and
  system architecture
- `docs/IMPLEMENTATION_PLAN.md` — Systems Engineer's build sequence,
  most-critical-first, ideally with a Mermaid diagram (renders natively
  on GitHub, is close enough to UML for this purpose without needing
  separate tooling)
- `docs/LOCKING.md` — the symbolic file-locking convention; read this
  before editing any binary asset or shared document

## Handoff protocol

Every handoff:
1. Removes the acting role's `agent:*` label
2. Adds exactly one new `agent:*` label for the next role
3. Adds a relevant `status:*` label alongside it when the handoff is
   anything other than the normal next step (an escalation, a
   cancellation, an RTVM-update fast path)

The workflow's job only reacts to label-*add* events, so a handoff always
means adding the next label — removing one on its own does nothing.
