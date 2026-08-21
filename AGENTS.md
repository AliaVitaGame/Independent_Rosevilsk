# Personal Starter Rules

Follow these rules on every task in this repository:

1. At the start of every new Codex session, read `.codex/session-start.txt` before doing any other work.
2. Write clean, readable, maintainable code.
3. Avoid code duplication (DRY). Extract shared logic when needed.
4. Use `var` when the type is obvious and readability is not reduced.
5. For Unity-related work, use Unity MCP tools when appropriate.
6. For Unity validation, prioritize `assets-refresh` (and Unity compile/log checks) before `dotnet build`. Use `dotnet build` as a secondary/fallback check.
7. For codebase, architecture, file relationship, or project structure questions, use Graphify when available:
   - If `graphify-out/graph.json` exists, query it first before broad manual exploration.
   - If no Graphify graph exists or obsolete stop here and offer to update 
   graphify then continiue.
   - After meaningful codebase structure changes, update the Graphify graph when appropriate.
8. Read Assets\_Core\AI Rules for more rules for the project (it might be empty)