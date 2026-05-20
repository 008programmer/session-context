# Claude.md

## Project Overview
This is a Dotnet 10 Blazor web app project
Project runs locally at `http://localhost:5232/users`

---

# Instructions

## General
- Follow existing project structure and conventions.
- Keep code simple and readable.
- Avoid unnecessary abstractions.
- Reuse existing utilities/components before creating new ones.
- Prefer consistency over personal preference.
- Do not run any `dotnet` command by yourself, I have already run it in watch mode, Ask me and wait for my confirmation

## Component Rules
- STOP before creating any new component file.
- Ask the user: "Should this go in BlazorApp-Context.Client (client) or BlazorApp-Context (server)?"
- You MUST NOT create the file until the user explicitly confirms the target project.
- Exception: skills under `.claude/skills/` may define their own placement policy (e.g. `add-blazor-page-from-api` defaults to `.Client/Pages`). Follow the skill when it applies.