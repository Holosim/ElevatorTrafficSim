#!/usr/bin/env bash
# Acquire a symbolic lock on a file before editing it.
#
# Usage: lock-acquire.sh <path> <holder-role> <issue-number> [reason]
#
# Exit 0 = lock acquired, safe to edit <path>
# Exit 1 = lock held by someone else (and not stale) -- back off,
#          see docs/LOCKING.md
#
# This is advisory, not an OS-level lock. It's race-free anyway
# because every acquire attempt ends in a git push, and git only
# accepts one fast-forward push at a time -- the loser's push is
# rejected. That rejection is the actual enforcement mechanism; the
# lock file's existence is just the human/agent-readable record of it.

set -euo pipefail

PATH_TO_LOCK="$1"
HOLDER="$2"
ISSUE="$3"
REASON="${4:-}"

LOCK_FILE=".claude/locks/${PATH_TO_LOCK}.lock"
STALE_AFTER_MINUTES=60
MAX_ATTEMPTS=3

git config user.name  >/dev/null 2>&1 || git config user.name  "agent-relay-bot"
git config user.email >/dev/null 2>&1 || git config user.email "agent-relay-bot@users.noreply.github.com"

BRANCH="$(git rev-parse --abbrev-ref HEAD)"

for attempt in $(seq 1 "$MAX_ATTEMPTS"); do
  git fetch origin >/dev/null 2>&1
  git reset --hard "origin/${BRANCH}" >/dev/null 2>&1

  if [[ -f "$LOCK_FILE" ]]; then
    ACQUIRED_AT="$(jq -r '.acquired_at' "$LOCK_FILE")"
    AGE_MIN=$(( ($(date -u +%s) - $(date -u -d "$ACQUIRED_AT" +%s)) / 60 ))
    if (( AGE_MIN < STALE_AFTER_MINUTES )); then
      CURRENT_HOLDER="$(jq -r '.holder' "$LOCK_FILE")"
      CURRENT_ISSUE="$(jq -r '.issue' "$LOCK_FILE")"
      echo "Locked by $CURRENT_HOLDER (issue #$CURRENT_ISSUE), age ${AGE_MIN}m -- not stale." >&2
      exit 1
    fi
    echo "Existing lock is ${AGE_MIN}m old (> ${STALE_AFTER_MINUTES}m) -- treating as abandoned." >&2
  fi

  mkdir -p "$(dirname "$LOCK_FILE")"
  jq -n \
    --arg path "$PATH_TO_LOCK" \
    --arg holder "$HOLDER" \
    --arg issue "$ISSUE" \
    --arg reason "$REASON" \
    --arg now "$(date -u +%Y-%m-%dT%H:%M:%SZ)" \
    '{path: $path, holder: $holder, issue: ($issue|tonumber), acquired_at: $now, reason: $reason}' \
    > "$LOCK_FILE"

  git add "$LOCK_FILE"
  git commit -m "Lock: $PATH_TO_LOCK ($HOLDER, issue #$ISSUE)" >/dev/null

  if git push origin "HEAD:${BRANCH}" >/dev/null 2>&1; then
    echo "Lock acquired: $PATH_TO_LOCK" >&2
    exit 0
  fi

  echo "Push rejected on attempt $attempt -- someone likely raced us. Retrying." >&2
  sleep $(( attempt * 3 ))
done

echo "Could not acquire lock on $PATH_TO_LOCK after $MAX_ATTEMPTS attempts." >&2
exit 1
