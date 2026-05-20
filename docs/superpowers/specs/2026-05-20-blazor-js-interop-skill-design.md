# Design: `blazor-js-interop` skill

**Date:** 2026-05-20
**Status:** Approved, pending implementation

## Purpose

A project-local Claude skill that guides JavaScript interop work in Blazor: calling JS from .NET, calling .NET from JS, picking the right load style, following the component lifecycle, and disposing every interop handle. Future-Claude reaches for it whenever an interop task appears, so the same rules apply every time and JS errors stop reaching the browser console.

## Skill metadata

- **Name:** `blazor-js-interop`
- **Location:** `.claude/skills/blazor-js-interop/SKILL.md`
- **Type:** Hybrid — discipline (six lifecycle rules) plus technique (two interop patterns)
- **Description (trigger):** Use when a Blazor component needs to call JavaScript (browser APIs, JS libraries) or receive callbacks from JS into .NET. Covers `IJSRuntime`, `IJSObjectReference`, `.razor.js` modules, `DotNetObjectReference`, `[JSInvokable]`, `OnAfterRenderAsync` timing, prerender safety, and disposal.
- **Activation keywords:** `IJSRuntime`, `InvokeAsync`, `[JSInvokable]`, "call JavaScript from", "from JS call C#", `window.something`, "module import", "ReconnectModal-style script", clipboard, chart, scroll-into-view, focus, localStorage, file download.

## Scope

In scope:
- Both load styles (static `<script type="module">` and JS Isolation via dynamic import).
- Full bidirectional interop (`IJSRuntime` calls + `DotNetObjectReference` callbacks).
- Component lifecycle rules: prerender ban, `OnAfterRenderAsync(firstRender)` gating, `IAsyncDisposable`, `JSDisconnectedException`, `StateHasChanged()` after callbacks.
- File placement: `.razor.js` co-located next to its `.razor` in the same project.
- Render-mode caveats: Server, WebAssembly, InteractiveAuto.
- Final verification via the existing `verify-blazor-page` skill.

Out of scope:
- Shared JS helpers under `wwwroot/js/*` — the user chose inline-only per component.
- Global `window.*` helpers — explicitly forbidden.
- A second skill for direction-specific interop — single skill covers both directions.

## Decision rule (the core technique)

| Use static `<script type="module" src="@Assets[...]">` | Use JS Isolation (`IJSRuntime.InvokeAsync<IJSObjectReference>("import", ...)`) |
|---|---|
| Self-contained JS; runs once at module parse | Component calls JS at runtime after first render |
| No C# → JS calls after parse | Per-instance JS state (chart, observer, editor) |
| No cleanup required | Cleanup required when component disposes |
| Matches existing `ReconnectModal.razor` | Standard pattern for component-scoped JS |

**One-line heuristic:** does the component need to talk to JS after first render, or clean up JS state when it goes away? Yes → JS isolation. No → static script.

**Forbidden:** `JSRuntime.InvokeAsync("window.something", ...)` against a global helper. Use a `.razor.js` module instead.

## The six lifecycle rules

1. **Never call JS during prerender or initialization.** No `IJSRuntime` from `OnInitializedAsync`, the constructor, or parameter setters. Always `OnAfterRenderAsync(bool firstRender)` with `if (firstRender)` for one-time setup.

2. **Import the module once, store the `IJSObjectReference`.** Path is relative to wwwroot/app base and points at the co-located `.razor.js`. Re-importing on every render is a bug.

3. **Implement `IAsyncDisposable` and dispose the module.** Wrap dispose in `try/catch (JSDisconnectedException)` — that one is the legitimate "circuit gone" case. Other exceptions bubble.

4. **`DotNetObjectReference` for JS → .NET callbacks, dispose it too.** `[JSInvokable]` method must be `public`. Call `StateHasChanged()` if the callback mutates state.

5. **InteractiveAuto/Server mode caveat.** All `InvokeAsync` calls are async over SignalR in Server mode. Never `.Result` / `.Wait()`. Don't assume `IJSInProcessRuntime` in InteractiveAuto.

6. **Co-locate the `.razor.js` next to its `.razor`, in the same project.** No asking — placement follows the component automatically. `.Client/Pages/Counter.razor` → `.Client/Pages/Counter.razor.js`. `.Server/Components/Pages/Foo.razor` → same folder.

## SKILL.md outline

1. YAML frontmatter (name, description)
2. Overview (core principle, 1 paragraph)
3. When to use / when NOT to use
4. Decision flowchart (small graphviz)
5. Quick reference table (pattern → when → minimum code)
6. Pattern A: Static `<script type="module">` — one complete example (~10 lines)
7. Pattern B: JS Isolation — one complete example applying all six rules (~30 lines)
8. The six rules as a numbered reference
9. File placement rule (co-locate, no asking)
10. Common mistakes table (symptoms → fixes)
11. Red flags — STOP (rationalization counters)
12. Verification gate (invoke `verify-blazor-page`)

## Common mistakes the skill addresses

| Symptom | Fix |
|---|---|
| `InvalidOperationException: JavaScript interop calls cannot be issued...` | Move call into `OnAfterRenderAsync(firstRender)`; gate with `if (firstRender)` |
| `JSDisconnectedException` thrown on dispose | Wrap `DisposeAsync` in `try/catch (JSDisconnectedException)` |
| JS callback updates field but UI doesn't refresh | Call `StateHasChanged()` from the `[JSInvokable]` method |
| Module re-imported every render | Add `if (firstRender)` gate around the import |
| `[JSInvokable]` method "not found" by JS | Method must be `public` |
| Tried `window.foo()` global | Move to a `.razor.js` module; drop the global |

## Red flags (rationalization counters)

- "Just one call, `window.*` is fine" → No. Use a `.razor.js` module.
- "I'll skip dispose for now" → No. JSDisconnectedException + leaked DotNetRef.
- "`OnInitialized` works since the component is interactive" → No. Prerender still runs.
- "It's WASM-only, sync is fine" → If the component is `InteractiveAuto`, it might run on Server. Always `await`.
- "I'll put the helper in `wwwroot/site.js` to share it" → No. Per-user choice: inline only.

## Verification gate

Skill is not complete until `verify-blazor-page` runs on the affected route and the browser console is clean. `dotnet build` passing does not count — JS errors surface only in the browser.

## Test plan (TDD cycle)

**RED — baseline scenario (without skill):**
Spawn a `general-purpose` subagent in a fresh worktree. Task:

> Add a "Copy to clipboard" button to `BlazorApp-Context.Client/Pages/Counter.razor` that copies the current count when clicked. After the copy attempt, JS must call back into C# to report success or failure, and the page must show a small "Copied!" or "Copy failed" status. The component runs in `InteractiveAuto` mode.

Document baseline behavior verbatim. Likely violations:
- JS placed in `wwwroot/site.js` with a `window.*` helper
- Call issued from `OnInitializedAsync` (prerender failure)
- No `IAsyncDisposable`
- No `JSDisconnectedException` catch
- Missing `StateHasChanged()` after callback
- No browser verification

**GREEN — write SKILL.md** addressing those exact violations.

**REFACTOR — re-test with the same task plus a simplicity-pressure variant:**

> Quick task — add a clipboard copy button to Counter with success/failure feedback. Just need it working, don't over-engineer.

Success criteria for the WITH-skill run, all six must hold:
1. JS lives in `Counter.razor.js` co-located in `.Client/Pages/`.
2. Uses JS Isolation (dynamic import), not static `<script>`.
3. JS called only from `OnAfterRenderAsync(firstRender: true)`.
4. Implements `IAsyncDisposable`; disposes `_module` with `JSDisconnectedException` catch; disposes `DotNetObjectReference`.
5. No `window.*` global, no `wwwroot/site.js`.
6. Invokes `verify-blazor-page` on `/counter`; console is clean.

If any criterion fails, REFACTOR (add an explicit counter to the rationalization that caused the miss) and re-test.

## Deliverables

- `.claude/skills/blazor-js-interop/SKILL.md` — the skill itself.
- This design doc (committed).
- A short note in `MEMORY.md`/memory only if a non-obvious decision emerges during testing.

## Open questions

None at design time. Testing may surface new rationalizations to add to the red-flags list.
