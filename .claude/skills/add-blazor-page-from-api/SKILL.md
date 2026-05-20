---
name: add-blazor-page-from-api
description: Use when the user asks to create a new Blazor page that fetches and displays data from a JSON HTTP API (e.g. dummyjson.com), wires it into the nav menu, and verifies it renders at http://localhost:5232
---

# Add Blazor Page From API

## Overview
End-to-end recipe for adding a data-driven page to this .NET 10 Blazor app: fetch JSON, render a model, register the route, link from the nav menu, then hand off to `verify-blazor-page` to confirm it works in the browser.

This skill assumes the project layout in CLAUDE.md (BlazorApp-Context = server host, BlazorApp-Context.Client = WebAssembly/interactive pages).

## When to Use
- "Create a page that shows X from API Y"
- "Add a users / products / posts page using dummyjson"
- Any request that combines: new route + HTTP fetch + table/list rendering

When NOT to use:
- Adding a non-routed component (use a regular component skill)
- Modifying an existing page's data source (just edit it)

## Placement policy
**Default to `BlazorApp-Context.Client/Pages/` unless the user says otherwise.** Confirm once at the start of the task, then proceed without re-asking for follow-up files in the same task.

This overrides the generic "always ask" rule in CLAUDE.md for this specific recipe — the user has pre-approved Client as the default for API-driven pages.

## Recipe

1. **Confirm placement once.** Say: "Creating in BlazorApp-Context.Client/Pages — say 'server' to put it in BlazorApp-Context/Components/Pages instead." Wait for objection only; proceed on silence.

2. **Inspect the API.** If you don't already know the response shape, fetch one sample (e.g. `https://dummyjson.com/users?limit=1`) and infer the model. Prefer `record` types for DTOs.

3. **Create the page** at `BlazorApp-Context.Client/Pages/<Name>.razor`. Follow the Counter.razor / Weather.razor conventions already in the repo:
   - `@page "/<route>"` directive
   - `@rendermode InteractiveAuto` (matches Counter.razor)
   - `@inject HttpClient Http` for fetching
   - `<PageTitle>`, `<h1>`, loading state, then table or list
   - Private `record` for the DTO inside `@code { }`

4. **Register HttpClient** if not already registered. Check `BlazorApp-Context.Client/Program.cs` for `builder.Services.AddScoped<HttpClient>(...)` or `AddHttpClient()`. If missing, add it with `BaseAddress` pointing at the API root.

5. **Link from nav.** Add a `<NavLink>` entry to `BlazorApp-Context/Components/Layout/NavMenu.razor` matching the existing pattern (`nav-item px-3` wrapper, icon span, NavLink with `href`).

6. **Hand off to verification.** Invoke the `verify-blazor-page` skill with the route (e.g. `/users`). Do not run `dotnet` yourself — CLAUDE.md says watch mode is already running.

## Reference: page template

Use Weather.razor's shape as the model. The minimal diff for a new API-backed page:

```razor
@page "/users"
@rendermode InteractiveAuto
@inject HttpClient Http

<PageTitle>Users</PageTitle>
<h1>Users</h1>

@if (users is null)
{
    <p><em>Loading...</em></p>
}
else
{
    <table class="table">
        <thead><tr><th>Name</th><th>Email</th></tr></thead>
        <tbody>
            @foreach (var u in users)
            {
                <tr><td>@u.firstName @u.lastName</td><td>@u.email</td></tr>
            }
        </tbody>
    </table>
}

@code {
    private User[]? users;

    protected override async Task OnInitializedAsync()
    {
        var resp = await Http.GetFromJsonAsync<UsersResponse>("https://dummyjson.com/users");
        users = resp?.users;
    }

    private record UsersResponse(User[] users);
    private record User(int id, string firstName, string lastName, string email);
}
```

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| Putting the page in `BlazorApp-Context/Components/Pages` by default | Default to `.Client/Pages` per this skill's placement policy |
| Running `dotnet build` / `dotnet run` to test | Forbidden by CLAUDE.md — watch mode is already running. Use `verify-blazor-page` instead |
| Forgetting the nav link | Page works at the URL but is undiscoverable. Always edit `NavMenu.razor` |
| Missing `@rendermode` | API call may not fire in static SSR. Match Counter.razor (`InteractiveAuto`) |
| Inventing model fields | Fetch one row and read the actual JSON before naming record properties |

## Red flags
- About to create the file without confirming placement → STOP, say the one-liner from step 1 first
- About to run a `dotnet` command → STOP, that's a CLAUDE.md violation
- Skipping the NavMenu edit "because the route works" → STOP, finish the recipe
