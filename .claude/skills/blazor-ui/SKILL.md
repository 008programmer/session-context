---
name: blazor-ui
description: Use this skill when creating any Blazor UI component (.razor files). Covers component structure, code-behind conventions, event handling, forms, styling, and placement rules.
---

# Blazor UI Skill

## Component Structure
- Use `.razor` file extension
- Code-behind in `.razor.cs` only if logic is complex
- Prefer `@code {}` block for simple components

## Conventions
- Use `EventCallback` not `Action` for parent-child events
- Inject services via `@inject`, not constructor
- Use `<EditForm>` with `DataAnnotationsValidator` for forms

## Styling
- Component-scoped CSS in `ComponentName.razor.css`
- Follow existing CSS variable/class naming in the project

## After Writing Component
- Remind user to confirm client vs server placement (per CLAUDE.md Component Rules)