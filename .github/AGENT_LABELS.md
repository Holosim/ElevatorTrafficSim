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
- `status:ready-for-commit` — tests passed, awaiting CI/CD
- `status:verified` — the linked RTVM item is closed

## Type labels

- `type:requirement` — traces to a specific RTVM line item
- `type:blocker` — a question raised by an agent, not a client-facing ask
- `type:bug`

## Title convention

Issues that trace to a feature start with the MVP ID:

```
[MVP-011] Short description of the feature
```

This makes the MVP ID searchable across issues, commits, and PRs without needing a label per ID. Features are the minimum viable product description for each enhancement of the software application that adds value to its function. The list of MVP's answers the question of "what" to make, and is the responsibility of the solutions-architect in coordination with the client (user).

Issues that trace to a requirement start with the RTVM ID:

```
[RTVM-012] Short description of the requirement
```

This makes the RTVM ID searchable across issues, commits, and PRs without needing a label per ID. The Requirements Traceability Verification Matrix is the documented description of HOW each desired MVP will be implemented, and WHERE a feature will reside in the system. It is the responsibility of the systems-engineeer to manage this matrix, along with pre-planned test procedures of how to verify that the feature functions correctly once it is completed.

Issues that trace to any user input, output, or interface start with the UI ID:

```
[UI-013] Short description of the User Interface
```

This makes the UI ID searchable across issues, commits, and PRs without needing a label per ID. The User Interface is implemented by the software-engineer, as specified in the RTVM by the systems-engineer, as defined by the solutions-architect.

All other issues start with the TASK ID:

```
[TASK-013] Short description of the task
```

This delineates the rest of the issues, commits, and PRs from the ones with a specific ID without needing a label.

## Handoff protocol

Every handoff:
1. Removes the acting role's `agent:*` label
2. Adds exactly one new `agent:*` label for the next role
3. Adds `status:blocked` too, if this is an escalation rather than a
   normal next step

The workflow's job only reacts to label-*add* events, so a handoff always
means adding the next label — removing one on its own does nothing.
