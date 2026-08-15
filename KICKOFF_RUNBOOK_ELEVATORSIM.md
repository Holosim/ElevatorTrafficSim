NB!  This Kickoff Runbook was never configured at the beginning of this project effort.
Future development should have this RUNBOOK align the current state of the rpoject with the new multi-agent strategy.


# Kickoff Runbook: Elevator Traffic Simulator

Self-contained setup steps for starting this project from the
`=TEMPLATE=` repo. Assumes GitHub CLI (`gh`) is installed and
authenticated, and that a Claude subscription (Pro/Max/Team) is
available. No prior context beyond this document is required.

---

## 0. Naming decision — read before step 1

There is already a local folder at `C:\_Dev\GIT\ElevatorTrafficSim`
from an earlier, unrelated project (an older, non-agent-built elevator
project whose documentation was used as source material when
`=TEMPLATE=` was first built). To avoid any collision — either on disk
or in naming intent — this runbook uses **`ElevatorTrafficSimAgent`**
as the new repo's name. Substitute your own preferred name throughout
if you'd rather; just keep it distinct from the existing folder.

---

## 1. Create the repository from `=TEMPLATE=`

On GitHub: open the `=TEMPLATE=` repo → **Use this template** →
**Create a new repository** → name it `ElevatorTrafficSimAgent` →
create.

Clone it locally:

```bash
cd C:\_Dev\GIT
git clone https://github.com/<your-username>/ElevatorTrafficSimAgent.git
cd ElevatorTrafficSimAgent
```

---

## 2. Secrets — none of these carry over from the template automatically

Go to the new repo's **Settings → Secrets and variables → Actions**.

### 2a. `CLAUDE_CODE_OAUTH_TOKEN` (this project uses your Claude
subscription, not separate API billing)

Locally:

```
claude setup-token
```

This opens a browser, logs in with your subscription account, and
prints a token starting `sk-ant-oat01-...`. Copy it immediately — it's
shown exactly once.

Back in **Secrets and variables → Actions → New repository secret**:
name it exactly `CLAUDE_CODE_OAUTH_TOKEN`, paste the value, save.

Do **not** add `ANTHROPIC_API_KEY` for this project — it's
intentionally being left off so the workflow has no fallback to
silently drift onto separate billing.

### 2b. `RELAY_TOKEN` — a fine-grained personal access token

This cannot be reused as-is from another repo if it was narrowly
scoped there. Either generate a fresh one, or edit an existing one's
repository access to include this new repo.

**To generate fresh:** GitHub → profile picture → **Settings** →
**Developer settings** → **Personal access tokens** → **Fine-grained
tokens** → **Generate new token**.
- Token name: `agent-relay-token` (or similar)
- Expiration: choose a long window, or no expiration — a short window
  caused a full, hard-to-diagnose outage on the prior project when it
  silently expired mid-project
- Repository access: **Only select repositories** → this repo only
- Permissions: **Contents** (Read and write), **Issues** (Read and
  write), **Pull requests** (Read and write)
- Generate, copy the value immediately (`github_pat_...`, shown once)

Store it the same way: **New repository secret**, name exactly
`RELAY_TOKEN`.

---

## 3. Confirm the GitHub App covers this repo

Settings → **GitHub Apps** → find the Claude app → **Configure**.
Under repository access, confirm `ElevatorTrafficSimAgent` is included
— either because it's set to all repositories, or because it's been
explicitly added to the selected list.

---

## 4. Confirm Actions are enabled

Settings → **Actions → General** → confirm "Allow all actions and
reusable workflows" (or an equivalent allow-list) is selected, not
"Disable actions."

---

## 5. Create the labels

```bash
gh auth login   # one-time, if not already authenticated
./scripts/setup-labels.sh
```

Confirm with `gh label list` — expect 22 labels, including
`agent:product-manager`.

---

## 6. Verify the credential line in `agent-relay.yml` — the one step
most likely to be wrong by default

Open `.github/workflows/agent-relay.yml` in the new repo and check the
`with:` block under the "Run Claude Code as..." step. It must read:

```yaml
with:
  claude_code_oauth_token: ${{ secrets.CLAUDE_CODE_OAUTH_TOKEN }}
  allowed_bots: "claude[bot]"
  show_full_output: "true"
  # anthropic_api_key: ${{ secrets.ANTHROPIC_API_KEY }}
```

If instead `anthropic_api_key` is the active (uncommented) line, swap
which one is commented out, then:

```bash
git add .github/workflows/agent-relay.yml
git commit -m "Use subscription OAuth token, not separate API billing"
git push
```

**Worth doing once, separately:** if this needed fixing here, the same
fix is worth pushing to `=TEMPLATE=` directly too, so the next project
doesn't hit the same ambiguity.

---

## 7. Submit the kickoff issue

On the new repo: **Issues → New issue → Project Kickoff** (the
template). This auto-applies the `agent:product-manager` label the
moment it's submitted — nothing else needs to be added by hand.

Paste the following into the "What are we building?" field:

> Build a discrete-event simulation of elevator traffic in a high-rise
> building, in C#.
>
> **Core mechanics:** each functional element of an elevator's
> operation is modeled as its own class, each with its own operating
> duration — door open/close, button response, acceleration to speed,
> deceleration to stop, floor-to-floor travel at full speed, and
> similar.
>
> **Configuration:** a config file specifies the number of floors in
> the building, the number of elevators in operation, and the
> distribution patterns of occupants entering and exiting.
>
> **Occupant types**, each with a distinct arrival/departure pattern
> rather than a single uniform population:
> - Shoppers — destined for lower-level stores, during standard store
>   hours (e.g. 10am–8pm)
> - Office workers — destined for mid-level business floors, during
>   regular work hours (e.g. 8am–6pm)
> - Residents — upper-level apartments, generally leaving mornings
>   before work hours and returning evenings after, with some overlap
>   against the other groups
>
> **Purpose:** stochastic analysis of occupant travel times — wait
> time for an elevator to arrive, plus travel time to the destination
> floor including intermediate stops for other occupants along the
> way. The analysis should support decisions like optimal elevator
> count, and experimentation with clustering techniques that group
> occupants with similar destinations onto the same elevator during
> high-volume periods.

Notice what's deliberately *not* specified: no IDE, no target platform
beyond "C#," no specific .NET version. Unlike the Sudoku project, this
brief doesn't state a Visual Studio or Windows-only requirement —
worth letting the Product Manager's interview genuinely surface
whether one exists, rather than assuming either way. If it turns out
there's no IDE-specific deliverable requirement, this project may
never need a separate Windows-verification workflow at all, since
.NET builds and runs natively on the Ubuntu runners the agents already
use — a real, potential difference from how the Sudoku project went,
not a certainty yet.

---

## 8. Where the interview happens, and how to answer it

The Product Manager's questions will appear as a comment on the
kickoff issue itself — check the **Issues** tab, open the issue,
scroll to the comments.

To reply: **just write a plain comment** — no special mention syntax,
no `@` tag, nothing beyond normal English. Comments no longer trigger
anything on their own; the label is what does that. So after posting
your reply:

1. Open the issue's **Labels** section (right sidebar)
2. Remove `agent:product-manager`
3. Immediately re-add `agent:product-manager`

That relabel is what actually wakes the agent back up to read what you
wrote. This will repeat for as many rounds as the interview takes —
same reply-then-retoggle action each time.

If a run seems to be taking a while, check the **Actions** tab for the
current status before assuming something's wrong.

---

## 9. What "done" with kickoff looks like

Once scope is fully defined and confirmed, the Product Manager will
close the kickoff issue and open a new one titled **"RTVM"**, labeled
`agent:systems-engineer` — this is normal, expected behavior, not an
error. That's where real requirements decomposition begins, and it
proceeds on its own from there without needing this runbook again.
