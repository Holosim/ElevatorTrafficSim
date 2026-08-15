#!/usr/bin/env bash
set -euo pipefail

# Creates the agent:*, status:*, and type:* labels that
# .github/workflows/agent-relay.yml and .github/AGENT_LABELS.md depend
# on. Safe to re-run — existing labels are updated, not duplicated.
#
# Requires the GitHub CLI, authenticated (gh auth login). Run from
# inside the target repo, or add --repo owner/name to every line below.

# Role labels — whose turn it is
gh label create "agent:solutions-architect" --color 5319e7 --description "Solutions Architect's turn to act" --force
gh label create "agent:systems-engineer"    --color 5319e7 --description "Systems Engineer's turn to act" --force
gh label create "agent:software-engineer"   --color 5319e7 --description "Software Engineer's turn to act" --force
gh label create "agent:test-engineer"       --color 5319e7 --description "Test Engineer's turn to act" --force
gh label create "agent:cicd"                --color 5319e7 --description "CI/CD's turn to act" --force

# Status labels — modifiers, not triggers
gh label create "status:in-progress"      --color fbca04 --description "An agent run is currently active" --force
gh label create "status:blocked"          --color d93f0b --description "Escalation — pairs with agent:solutions-architect" --force
gh label create "status:ready-for-test"   --color 1d76db --description "Implementation done, awaiting Test Engineer" --force
gh label create "status:ready-for-rtvm-update" --color bfd4f2 --description "Test passed; Systems Engineer updates RTVM, then passes to CI/CD" --force
gh label create "status:ready-for-commit" --color 0e8a16 --description "Tests passed, awaiting CI/CD" --force
gh label create "status:verified"         --color 2cbe4e --description "Linked RTVM item is closed" --force
gh label create "status:cancelled"        --color 999999 --description "Test iteration voided by an RTVM/procedure change; restarts on next build" --force
gh label create "status:needs-human"      --color b60205 --description "Automated escalation exhausted; a human needs to look at this" --force
gh label create "status:waiting-on-lock"  --color fef2c0 --description "Backed off after failing to acquire a file lock" --force

# Type labels
gh label create "type:requirement" --color c5def5 --description "Traces to an RTVM line item" --force
gh label create "type:blocker"     --color e99695 --description "A question raised by an agent, not a client-facing ask" --force
gh label create "type:bug"         --color ee0701 --description "A defect, not a new requirement" --force

echo "Done — run 'gh label list' to confirm all 17 are there."
