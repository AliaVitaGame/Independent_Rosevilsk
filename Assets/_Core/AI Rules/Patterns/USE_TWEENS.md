> **Applicability:** Read the rest of this document only when the task involves
> mathematical animations, delays, timed transitions, interpolation, or other
> time-based behavior that should use PrimeTween. If the task does not involve
> any of these, stop here and do not apply or read the remaining rules.

# Use PrimeTween for animations

Use **PrimeTween** for all time-based and mathematical animations in Unity.
Animate values with tweens instead of implementing interpolation with a
coroutine, `Update`, or manual `Lerp` loops.

## Rules

- Add `using PrimeTween;` and use the appropriate `Tween` method.
- Prefer the most specific API available: `Tween.Position`,
  `Tween.LocalPosition`, `Tween.Scale`, `Tween.Rotation`, `Tween.Color`, etc.
- Specify the duration and easing explicitly. Use an easing that matches the
  intended motion instead of relying on an implicit default.
- Use `Sequence` with `Chain` and `Group` when several animations must be
  ordered or played together.
- Store a `Tween` when the animation may need to be stopped, completed, or
  replaced. Stop an existing tween before starting a new one on the same
  property when overlapping animations would be incorrect.
- Do not create a coroutine only to animate a value over time. Use
  `Tween.Custom` for values that do not have a dedicated PrimeTween method.
- A coroutine is still allowed for non-animation gameplay flow when it is the
  clearest tool, but interpolation and visual transitions belong in PrimeTween.

## Examples

```csharp
using PrimeTween;
using UnityEngine;

public sealed class PanelAnimation : MonoBehaviour {
    [SerializeField] private RectTransform _panel;

    private Tween _scaleTween;

    public void Show() {
        _scaleTween.Stop();
        _scaleTween = Tween.Scale(
            _panel,
            endValue: Vector3.one,
            duration: 0.25f,
            ease: Ease.OutBack);
    }
}
```

For ordered or parallel animation, use a sequence:

```csharp
var sequence = Sequence.Create()
    .Chain(Tween.LocalPositionY(transform, 1f, 0.3f, Ease.OutCubic))
    .Group(Tween.Scale(transform, Vector3.one, 0.3f, Ease.OutBack))
    .ChainDelay(0.1f)
    .Chain(Tween.LocalPositionY(transform, 0f, 0.2f, Ease.InCubic));
```

For a custom numeric value, use `Tween.Custom`:

```csharp
_tween = Tween.Custom(
    this,
    startValue: 0f,
    endValue: 1f,
    duration: 0.5f,
    ease: Ease.InOutSine,
    onValueChange: (target, value) => target.SetProgress(value));
```

Keep tween targets valid for the whole animation and stop or complete owned
tweens when the owning object is disabled or destroyed if the lifecycle is
not handled automatically by the target.
