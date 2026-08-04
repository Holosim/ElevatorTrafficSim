# File locking convention

Concurrency across issues is the point of this workflow — many
features can be in flight at once, each owned by exactly one role at a
time. That's fine for ordinary source files, since different features
usually touch different files, and CI/CD's branch strategy absorbs the
rest at merge time.

It is not fine for a resource two agents might edit at the same
*moment*, especially when git can't merge the result. This is a
**symbolic lock** — an agreed-upon convention, enforced by git's own
atomic-push behavior, not an OS-level file lock. Nothing stops an
agent from ignoring it except the convention itself, so every role
that touches a lockable resource needs to actually follow this.

## When to lock

**Mandatory:**
- Any binary asset — 3D models, textures, audio, Unreal `.uasset`
  files, anything git can't three-way-merge. A conflict on a binary
  file isn't something you resolve with conflict markers; it's silent
  data loss for whoever lost the race.

**Recommended:**
- Shared, frequently-edited text documents multiple roles write to —
  `docs/RTVM.md`, `docs/SDD.md`. Git can usually auto-merge
  non-overlapping text edits, so this is a courtesy against lost
  updates and avoidable conflicts, not a hard requirement.

**Not needed:**
- Ordinary source files scoped to one feature's own work. Different
  features usually touch different files; branch isolation and normal
  review at CI/CD are enough. Locking everything would cancel out the
  concurrency this workflow exists for.

## How it works

1. Before editing a lockable resource:
   `./scripts/lock-acquire.sh <path> <your-role> <issue-number> "<reason>"`
2. Exit 0 — you hold the lock. Proceed.
3. Exit 1 — someone else holds it (and it isn't stale). Comment on
   your own issue noting what you're blocked on and who holds it, add
   `status:waiting-on-lock`, and stop there. A scheduled sweep
   (`.github/workflows/lock-retry.yml`) retries you periodically — you
   don't need to poll or wait inside the job.
4. When you're done — or handing off — release it:
   `./scripts/lock-release.sh <path>`

Locks live at `.claude/locks/<path>.lock` as small JSON files, tracked
in git (`ls .claude/locks/` shows everything currently held). A lock
older than 60 minutes is treated as abandoned and can be taken by the
next agent that asks — long enough to outlast a normal job, short
enough that a crashed run can't deadlock a resource indefinitely.

## Why this is actually race-free

Two agents racing for the same lock both try to commit and push a
lock file claiming it. Git only accepts one of those pushes — the
second is rejected as a non-fast-forward, which is what the script
treats as "someone beat you to it." The lock file's existence is just
the readable record; the real enforcement is git refusing the second
write.

## What this doesn't solve

Locking prevents two agents from editing the *same* resource at the
same moment. It doesn't replace branch isolation for ordinary code, and
it doesn't make a later merge of *unrelated* changes conflict-free —
that's still CI/CD's job. Think of it as a narrow, specific tool for
the specific case where a collision would be silent and unrecoverable,
not a general substitute for the rest of the pipeline.
