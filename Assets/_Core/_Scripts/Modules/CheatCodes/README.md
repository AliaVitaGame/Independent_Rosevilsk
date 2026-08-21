# Cheat Codes (Isolated Feature)

This module is designed as an isolated Editor-only feature.
You can copy/connect it without touching gameplay logic.

## What This Feature Includes
- Attribute-based cheat declaration: `[CheatCode(...)]`
- Reflection registry: `CheatCodeRegistry`
- Runtime cheat UI: `CheatCodesCanvasBootstrapper`
- Button generation + invocation: `MethodButtonGenerator`

Main folder:
- `Assets/Scripts/Systems/CheatCodes/`

## Required Integration Points

### 1. Register services in your installer (Editor only)
In your game scope installer (for this project: `GameInstaller`), add:

```csharp
#if UNITY_EDITOR
private void RegisterCheets(IContainerBuilder builder)
{
    builder.Register<ICheatCodeRegistry, CheatCodeRegistry>(Lifetime.Scoped)
        .As<ICheatCodeRegistry, IServicePreloader, IDisposable>();

    builder.Register<CheatCodesCanvasBootstrapper>(Lifetime.Scoped)
        .As<ICheatCodesRuntimeUi, IDisposable>();

    // Optional: your cheat command classes
    builder.Register<GameCheatCodeCommands>(Lifetime.Scoped).AsSelf();
    builder.Register<CheatCodeUsageExamples>(Lifetime.Scoped).AsSelf();
}
#endif
```

And call it from `Configure(...)` inside `#if UNITY_EDITOR`.

### 2. Initialize from startup flow
In your startup initializer (for this project: `GameInitializer`):
- call `await _cheatCodeRegistry.WarmUp();`
- then call `_cheatCodesRuntimeUi.Initialize();`

This should happen after core gameplay services are initialized.

### 3. Add cheat methods
Create any class in `Assembly-CSharp` and add parameterless methods with `[CheatCode]`.

```csharp
using UnityEngine;

namespace GameTest
{
    public sealed class MyCheats
    {
        [CheatCode("Give Cash", "Economy", order: -10, categoryOrder: 0)]
        private static void GiveCash()
        {
            Debug.Log("[Cheat] Cash granted");
        }
    }
}
```

## Attribute Parameters
`CheatCode(readableName, category, order, categoryOrder)`
- `readableName`: button text
- `category`: group name
- `order`: order inside category (lower = higher)
- `categoryOrder`: order of categories (lower = higher)

## Runtime Behavior
- Toggle UI with `~` (BackQuote)
- Categories are shown as section headers
- Non-static cheat methods are resolved via:
  1. scene `MonoBehaviour` targets
  2. DI container fallback

## Constraints
- Method must be parameterless
- Reflection scan is limited to `Assembly-CSharp`
- Intended for Unity Editor (`#if UNITY_EDITOR`)

## Quick Validation Checklist
1. Enter Play Mode
2. Press `~`
3. See cheat panel on the left
4. Click any cheat button
5. Verify effect/log in Console
