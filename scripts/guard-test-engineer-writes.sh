#!/usr/bin/env bash
# PreToolUse hook, scoped to the test-engineer agent only (see the
# hooks: block in .claude/agents/test-engineer.md).
#
# Blocks every Edit/Write call except ones targeting this agent's own
# memory file. The point: a code change must always originate from,
# and be visible to, the Software Engineer -- Test Engineer reports
# problems, it never patches around them.

set -euo pipefail

ALLOWED_PREFIX=".claude/agent-memory/test-engineer/"

INPUT="$(cat)"
FILE_PATH="$(echo "$INPUT" | jq -r '.tool_input.file_path // empty')"

# Resolve to a repo-relative path regardless of whether Claude Code
# passed it as absolute or already-relative.
PROJECT_DIR="${CLAUDE_PROJECT_DIR:-$PWD}"
REL_PATH="${FILE_PATH#"$PROJECT_DIR"/}"

if [[ "$REL_PATH" == "$ALLOWED_PREFIX"* ]]; then
  exit 0
fi

cat >&2 <<EOF
Blocked: Test Engineer does not modify the codebase or any project
file other than its own memory log. Attempted path: ${FILE_PATH:-<none given>}
Allowed prefix: $ALLOWED_PREFIX

If you found a problem, describe it in your issue comment and hand
off to the Software Engineer -- don't edit it yourself.
EOF
exit 2
