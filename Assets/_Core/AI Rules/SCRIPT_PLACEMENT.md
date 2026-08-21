# Place scripts by responsibility

> **Applicability:** Apply this rule before creating or moving any runtime C# script.

Choose the narrowest architectural layer that owns the behaviour. Do not use
`Shared` as a default location.

| Responsibility | Location |
| --- | --- |
| Game-specific gameplay, rules, entities and systems | `Assets/_Core/_Scripts/Game/Core/` |
| Game-wide meta services such as analytics, lifecycle, networking and platform state | `Assets/_Core/_Scripts/Game/Meta/` |
| Independent feature systems that can grow or receive SDK integrations | `Assets/_Core/_Scripts/Modules/` |
| Reusable UI, utilities, generic contracts and infrastructure with no game or feature ownership | `Assets/_Core/_Scripts/Shared/` |

## Rules

- Create a feature folder inside its owning layer, for example
  `Modules/Audio` or `Game/Meta/Analytics`.
- Keep the namespace aligned with the layer and feature folder, for example
  `Game.Core.*`, `Game.Meta.*`, `Modules.*` or `Shared.*`.
- Put a contract beside the implementation it belongs to. Move it to
  `Shared` only when it is genuinely reused by multiple unrelated layers.
- Keep scene composition and game-start flow in `Bootstrap`; do not move them
  to `Shared` merely because they are globally registered.
- If a script combines responsibilities from several layers, split it before
  adding new feature code.
