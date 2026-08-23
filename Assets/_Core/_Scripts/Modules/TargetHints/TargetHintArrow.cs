using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.TargetHints
{
    /// <summary>
    /// Screen-edge direction arrow that switches to a world-follow marker
    /// when the target is inside the camera frustum.
    /// After staying in world mode for a while, the arrow fades out until
    /// it is needed on the canvas edge again.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class TargetHintArrow : MonoBehaviour
    {
        private enum HintMode
        {
            Hidden,
            ScreenEdge,
            WorldFollow
        }

        [Header("Bindings")]
        [SerializeField] private TargetHintTarget _target;
        [SerializeField] private RectTransform _arrowRect;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Camera _camera;

        [Header("Screen Edge")]
        [SerializeField] private float _edgePadding = 48f;
        [SerializeField] private float _viewportMargin = 0.05f;

        [Header("World Follow")]
        [SerializeField] private float _worldArrowRotationZ = 180f;
        [SerializeField] private float _worldVisibleDuration = 5f;
        [SerializeField] private float _fadeDuration = 0.45f;

        [Header("Kind Visuals")]
        [SerializeField] private Image _arrowImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Color _vehicleColor = new(1f, 0.82f, 0.18f, 0.9f);
        [SerializeField] private Color _passengerColor = new(1f, 0.15f, 0.12f, 0.9f);
        [SerializeField] private Color _exitColor = new(0.2f, 0.92f, 0.42f, 0.9f);

        private Canvas _parentCanvas;
        private Camera _uiCamera;
        private HintMode _mode = HintMode.Hidden;
        private TargetHintKind _appliedKind = (TargetHintKind)(-1);
        private Tween _alphaTween;
        private Sequence _worldFadeSequence;
        private Sprite _vehicleIcon;
        private Sprite _passengerIcon;
        private Sprite _exitIcon;

        public TargetHintTarget Target => _target;

        private void Awake()
        {
            if (_arrowRect == null)
                _arrowRect = (RectTransform)transform;

            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

            _parentCanvas = GetComponentInParent<Canvas>();
            EnsureVisualBindings();
            ResolveCameras();
            SetAlphaImmediate(0f);
        }

        private void OnEnable()
        {
            if (_target != null)
                _target.HintEnabledChanged += OnHintEnabledChanged;
        }

        private void OnDisable()
        {
            if (_target != null)
                _target.HintEnabledChanged -= OnHintEnabledChanged;

            StopVisibilityTweens();
        }

        public void SetTarget(TargetHintTarget target)
        {
            if (_target != null)
                _target.HintEnabledChanged -= OnHintEnabledChanged;

            _target = target;

            if (isActiveAndEnabled && _target != null)
                _target.HintEnabledChanged += OnHintEnabledChanged;

            ApplyKindVisuals(true);
        }

        private void LateUpdate()
        {
            if (_target == null || !_target.IsHintEnabled)
            {
                SetMode(HintMode.Hidden);
                return;
            }

            ResolveCameras();
            if (_camera == null || _parentCanvas == null)
            {
                SetMode(HintMode.Hidden);
                return;
            }

            var worldPos = _target.WorldPosition;
            var viewport = _camera.WorldToViewportPoint(worldPos);
            var inFront = viewport.z > 0f;
            var inView = inFront
                && viewport.x > _viewportMargin
                && viewport.x < 1f - _viewportMargin
                && viewport.y > _viewportMargin
                && viewport.y < 1f - _viewportMargin;

            if (inView)
                ApplyWorldFollow(worldPos);
            else
                ApplyScreenEdge(viewport, inFront);
        }

        private void ApplyWorldFollow(Vector3 worldPos)
        {
            SetMode(HintMode.WorldFollow);

            var screenPoint = _camera.WorldToScreenPoint(worldPos);
            if (!TryScreenToCanvas(screenPoint, out var localPoint))
            {
                SetMode(HintMode.Hidden);
                return;
            }

            _arrowRect.anchoredPosition = localPoint;
            _arrowRect.localRotation = Quaternion.Euler(0f, 0f, _worldArrowRotationZ);
            KeepIconUpright();
            ApplyKindVisuals(false);
        }

        private void ApplyScreenEdge(Vector3 viewport, bool inFront)
        {
            SetMode(HintMode.ScreenEdge);

            if (!inFront)
            {
                viewport.x = 1f - viewport.x;
                viewport.y = 1f - viewport.y;
            }

            var screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            var screenPoint = new Vector2(viewport.x * Screen.width, viewport.y * Screen.height);
            var direction = screenPoint - screenCenter;
            if (direction.sqrMagnitude < 0.001f)
                direction = Vector2.up;

            direction.Normalize();

            var halfSize = new Vector2(
                Screen.width * 0.5f - _edgePadding,
                Screen.height * 0.5f - _edgePadding);

            var edgePoint = screenCenter + ClampToScreenEdge(direction, halfSize);
            if (!TryScreenToCanvas(edgePoint, out var localPoint))
            {
                SetMode(HintMode.Hidden);
                return;
            }

            _arrowRect.anchoredPosition = localPoint;

            // Sprite points up (local +Y). Rotate so tip faces the off-screen target.
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            _arrowRect.localRotation = Quaternion.Euler(0f, 0f, angle);
            KeepIconUpright();
            ApplyKindVisuals(false);
        }

        private bool TryScreenToCanvas(Vector2 screenPoint, out Vector2 localPoint)
        {
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)_parentCanvas.transform,
                screenPoint,
                _uiCamera,
                out localPoint);
        }

        private static Vector2 ClampToScreenEdge(Vector2 direction, Vector2 halfSize)
        {
            var absX = Mathf.Abs(direction.x);
            var absY = Mathf.Abs(direction.y);

            if (absX * halfSize.y > absY * halfSize.x)
            {
                var x = Mathf.Sign(direction.x) * halfSize.x;
                var y = direction.y / absX * halfSize.x;
                return new Vector2(x, y);
            }

            var edgeY = Mathf.Sign(direction.y) * halfSize.y;
            var edgeX = direction.x / absY * halfSize.y;
            return new Vector2(edgeX, edgeY);
        }

        private void SetMode(HintMode mode)
        {
            if (_mode == mode)
                return;

            _mode = mode;

            switch (mode)
            {
                case HintMode.ScreenEdge:
                    StopVisibilityTweens();
                    FadeToAlpha(1f);
                    break;

                case HintMode.WorldFollow:
                    StopVisibilityTweens();
                    FadeToAlpha(1f);
                    ScheduleWorldFadeOut();
                    break;

                case HintMode.Hidden:
                    StopVisibilityTweens();
                    SetAlphaImmediate(0f);
                    break;
            }
        }

        private void ScheduleWorldFadeOut()
        {
            _worldFadeSequence = Sequence.Create()
                .ChainDelay(_worldVisibleDuration)
                .Chain(Tween.Alpha(_canvasGroup, endValue: 0f, duration: _fadeDuration, ease: Ease.OutSine));
        }

        private void FadeToAlpha(float alpha)
        {
            _alphaTween.Stop();
            _alphaTween = Tween.Alpha(
                _canvasGroup,
                endValue: alpha,
                duration: _fadeDuration,
                ease: Ease.OutSine);
        }

        private void SetAlphaImmediate(float alpha)
        {
            StopVisibilityTweens();
            _canvasGroup.alpha = alpha;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        private void StopVisibilityTweens()
        {
            _alphaTween.Stop();
            _worldFadeSequence.Stop();
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        private void ResolveCameras()
        {
            if (_camera == null)
                _camera = Camera.main;

            if (_parentCanvas == null)
                _parentCanvas = GetComponentInParent<Canvas>();

            if (_parentCanvas == null)
            {
                _uiCamera = null;
                return;
            }

            _uiCamera = _parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : _parentCanvas.worldCamera;
        }

        private void OnHintEnabledChanged(TargetHintTarget _)
        {
            if (_target == null || !_target.IsHintEnabled)
                SetMode(HintMode.Hidden);
        }

        private void KeepIconUpright()
        {
            if (_iconImage == null)
                return;

            _iconImage.rectTransform.localRotation = Quaternion.Inverse(_arrowRect.localRotation);
        }

        private void ApplyKindVisuals(bool force)
        {
            if (_target == null)
                return;

            var kind = _target.Kind;
            if (!force && kind == _appliedKind)
                return;

            _appliedKind = kind;
            if (_arrowImage != null)
                _arrowImage.color = GetColor(kind);

            if (_iconImage != null)
            {
                _iconImage.sprite = GetIcon(kind);
                _iconImage.enabled = _iconImage.sprite != null;
                _iconImage.color = Color.white;
            }
        }

        private Color GetColor(TargetHintKind kind)
        {
            return kind switch
            {
                TargetHintKind.Vehicle => _vehicleColor,
                TargetHintKind.Exit => _exitColor,
                _ => _passengerColor
            };
        }

        private Sprite GetIcon(TargetHintKind kind)
        {
            switch (kind)
            {
                case TargetHintKind.Vehicle:
                    return _vehicleIcon ??= TargetHintIconFactory.Create(TargetHintKind.Vehicle);
                case TargetHintKind.Exit:
                    return _exitIcon ??= TargetHintIconFactory.Create(TargetHintKind.Exit);
                default:
                    return _passengerIcon ??= TargetHintIconFactory.Create(TargetHintKind.Passenger);
            }
        }

        private void EnsureVisualBindings()
        {
            if (_arrowImage == null)
                _arrowImage = GetComponentInChildren<Image>(true);

            if (_iconImage != null)
                return;

            var iconGo = new GameObject("HintIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconGo.transform.SetParent(_arrowRect, false);
            var iconRect = (RectTransform)iconGo.transform;
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(28f, 28f);
            iconRect.anchoredPosition = Vector2.zero;
            _iconImage = iconGo.GetComponent<Image>();
            _iconImage.raycastTarget = false;
            _iconImage.preserveAspect = true;
        }
    }
}
