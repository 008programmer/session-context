---
name: verify-blazor-page
description: Use when a Blazor page or component change needs to be confirmed working in the browser — navigates to a route on http://localhost:5232, captures a DOM snapshot and PNG screenshot, and checks for console errors
---

# Verify Blazor Page

## Overview
Runs a browser-based verification pass against the locally running Blazor app using the Playwright MCP tools. Produces visual + structural evidence that a change actually works — required before claiming a task is complete (per superpowers:verification-before-completion).

The app runs at `http://localhost:5232` in watch mode. Do not start or restart it.

## When to Use
- Just finished adding a page, component, or nav entry — need to confirm it renders
- A bug fix that changes UI behavior — need to see the fixed state
- Pair this with `add-blazor-page-from-api` as the verification step
- Any time you're about to say "done" about a Blazor UI change

When NOT to use:
- Pure backend/Program.cs changes with no UI surface (run the relevant unit test instead)
- The dev server isn't running (ask the user to start it; do not run `dotnet` yourself)

## Inputs
- **route** (required) — path to verify, e.g. `/users`, `/counter`
- **expected_heading** (optional) — text expected on the page (e.g. `Users`) for a soft sanity check

## Procedure

1. **Navigate.** `mcp__plugin_playwright_playwright__browser_navigate` to `http://localhost:5232<route>`.
2. **Wait briefly for interactive render.** Use `browser_wait_for` with the expected heading text if provided; otherwise wait ~1s for InteractiveAuto components to hydrate.
3. **Snapshot.** Call `browser_snapshot` to capture the accessibility tree. This is the primary evidence.
4. **Screenshot.** Call `browser_take_screenshot` (PNG). Save as visual proof.
5. **Check console.** Call `browser_console_messages` and scan for errors. Warnings about Blazor reconnect/dev-mode are OK; surface anything else.
6. **Report.** Output to the user:
   - Route checked + status (rendered / error)
   - Heading found (if expected_heading was given)
   - Console error count (zero = green)
   - Screenshot reference

## What counts as verified

All three must hold:
- Snapshot includes the expected page content (heading, table, etc. — not just the layout shell)
- Screenshot is non-blank (page didn't render as empty body)
- No unexpected console errors

If any fail: report the failure precisely, do NOT claim success. Hand back to the implementation step.

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| Saying "looks good" without taking a screenshot | This skill requires both snapshot AND screenshot — they catch different failure modes |
| Treating a 200 response as success | The page can return 200 with a runtime error visible only in console/DOM |
| Ignoring console errors as "probably fine" | Surface them. Blazor JS errors silently break interactivity |
| Skipping the wait | InteractiveAuto pages render shell first, hydrate later. Snapshot-too-early = false negative |
| Starting/restarting the dev server | Forbidden by CLAUDE.md. Ask the user if the port isn't responding |

## Red flags
- About to claim "the page works" without calling `browser_snapshot` → STOP, take the snapshot
- About to call `dotnet run` because the page isn't loading → STOP, ask the user
- Console shows errors but you're reporting success → STOP, that's not verified
