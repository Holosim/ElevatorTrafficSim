#!/usr/bin/env bash
# Release a symbolic lock previously acquired with lock-acquire.sh.
#
# Usage: lock-release.sh <path>

set -euo pipefail

PATH_TO_LOCK="$1"
LOCK_FILE=".claude/locks/${PATH_TO_LOCK}.lock"

git config user.name  >/dev/null 2>&1 || git config user.name  "agent-relay-bot"
git config user.email >/dev/null 2>&1 || git config user.email "agent-relay-bot@users.noreply.github.com"

if [[ ! -f "$LOCK_FILE" ]]; then
  echo "No lock file at $LOCK_FILE -- nothing to release." >&2
  exit 0
fi

BRANCH="$(git rev-parse --abbrev-ref HEAD)"

git rm -q "$LOCK_FILE"
git commit -m "Unlock: $PATH_TO_LOCK" >/dev/null

for attempt in 1 2 3; do
  if git push origin "HEAD:${BRANCH}" >/dev/null 2>&1; then
    echo "Lock released: $PATH_TO_LOCK" >&2
    exit 0
  fi
  git fetch origin >/dev/null 2>&1
  git rebase "origin/${BRANCH}" >/dev/null 2>&1 || true
  sleep $(( attempt * 2 ))
done

echo "Could not push the unlock for $PATH_TO_LOCK -- resolve manually." >&2
exit 1
