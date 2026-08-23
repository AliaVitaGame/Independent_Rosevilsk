using Game.Core.Mission;
using Game.Core.Vehicles;
using HEAVYART.TopDownShooter.Netcode;
using Modules.TargetHints;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Core.Map
{
    /// <summary>
    /// Full-city top-down map overlay, toggled with M or N.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CityMapController : MonoBehaviour
    {
        [SerializeField] private KeyCode _toggleKeyA = KeyCode.M;
        [SerializeField] private KeyCode _toggleKeyB = KeyCode.N;
        [SerializeField] private float _cameraHeight = 280f;
        [SerializeField] private float _minOrthoSize = 40f;
        [SerializeField] private float _zoomSpeed = 80f;
        [SerializeField] private float _panSpeed = 1.15f;
        [SerializeField] private float _keyboardPanSpeed = 180f;
        [SerializeField] private Vector3 _fallbackCityCenter = new(-80f, 0f, 40f);
        [SerializeField] private float _fallbackCitySize = 260f;

        private Camera _mapCamera;
        private Camera _mainCamera;
        private Canvas _overlayCanvas;
        private RectTransform _playerMarker;
        private readonly System.Collections.Generic.List<RectTransform> _targetMarkers = new();
        private DriveableVehicleInteraction _vehicle;
        private MissionCompleteUI _missionUi;
        private GameCameraController _gameCamera;
        private Transform _player;
        private bool _isOpen;
        private bool _lockedCamera;
        private bool _dragging;
        private float _savedTimeScale = 1f;
        private float _orthoSize;
        private float _maxOrthoSize;
        private Vector3 _lookPoint;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            _vehicle = FindFirstObjectByType<DriveableVehicleInteraction>();
            _missionUi = FindFirstObjectByType<MissionCompleteUI>(FindObjectsInactive.Include);
            EnsureCamera();
            EnsureOverlay();
            SetOpen(false, true);
        }

        private void OnDisable()
        {
            if (_isOpen)
                SetOpen(false, true);
        }

        private void Update()
        {
            if (_missionUi == null)
                _missionUi = FindFirstObjectByType<MissionCompleteUI>(FindObjectsInactive.Include);
            if (_missionUi != null && _missionUi.IsConfirmVisible)
                return;

            if (Input.GetKeyDown(_toggleKeyA) || Input.GetKeyDown(_toggleKeyB))
                SetOpen(!_isOpen, false);

            if (!_isOpen)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetOpen(false, false);
                return;
            }

            HandleZoom();
            HandlePan();
            ApplyCamera();
        }

        private void LateUpdate()
        {
            if (_isOpen)
                UpdateMarkers();
        }

        private void SetOpen(bool open, bool immediate)
        {
            if (_isOpen == open && !immediate)
                return;

            _isOpen = open;
            if (_mapCamera != null)
                _mapCamera.enabled = open;

            if (_overlayCanvas != null)
                _overlayCanvas.gameObject.SetActive(open);

            if (open)
            {
                _savedTimeScale = Time.timeScale <= 0.001f ? 1f : Time.timeScale;
                Time.timeScale = 0f;
                _mainCamera = Camera.main;
                if (_mapCamera != null && _mainCamera != null)
                    _mapCamera.cullingMask = _mainCamera.cullingMask;
                _gameCamera = _mainCamera != null
                    ? _mainCamera.GetComponent<GameCameraController>()
                    : FindFirstObjectByType<GameCameraController>();
                _gameCamera?.StopCameraMovement();
                _lockedCamera = true;
                _vehicle?.SetGameplayLocked(true);
                _vehicle?.SetCameraLocked(true);
                ComputeCityFrame();
                _lookPoint = ResolveOrigin();
                _lookPoint.y = 0f;
                ApplyCamera();
            }
            else if (_lockedCamera || immediate)
            {
                Time.timeScale = Mathf.Max(0.001f, _savedTimeScale);
                _vehicle?.SetGameplayLocked(false);
                _vehicle?.SetCameraLocked(false);
                _gameCamera?.ActivateCameraMovement();
                _lockedCamera = false;
            }
        }

        private void HandleZoom()
        {
            var scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) < 0.01f)
                return;

            _orthoSize = Mathf.Clamp(_orthoSize - scroll * _zoomSpeed, _minOrthoSize, _maxOrthoSize);
        }

        private void HandlePan()
        {
            if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
                _dragging = true;

            if (Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2))
                _dragging = false;

            if (_dragging)
            {
                var delta = new Vector3(-Input.GetAxisRaw("Mouse X"), 0f, -Input.GetAxisRaw("Mouse Y"));
                _lookPoint += delta * (_orthoSize * _panSpeed * 0.08f);
            }

            var keyboard = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            if (keyboard.sqrMagnitude > 0.01f)
                _lookPoint += keyboard.normalized * (_keyboardPanSpeed * Time.unscaledDeltaTime * (_orthoSize / 120f));
        }

        private void ApplyCamera()
        {
            if (_mapCamera == null)
                return;

            _mapCamera.orthographicSize = _orthoSize;
            _mapCamera.transform.position = new Vector3(_lookPoint.x, _cameraHeight, _lookPoint.z);
            _mapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        private void ComputeCityFrame()
        {
            var bounds = new Bounds(_fallbackCityCenter, Vector3.one);
            var hasBounds = false;
            var containers = FindObjectsByType<FCG.FCGWaypointsContainer>();
            for (var i = 0; i < containers.Length; i++)
            {
                var points = containers[i].waypoints;
                if (points == null)
                    continue;

                for (var p = 0; p < points.Count; p++)
                {
                    if (points[p] == null)
                        continue;

                    if (!hasBounds)
                    {
                        bounds = new Bounds(points[p].position, Vector3.one * 8f);
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(points[p].position);
                    }
                }
            }

            var size = hasBounds
                ? Mathf.Max(bounds.extents.x, bounds.extents.z) + 40f
                : _fallbackCitySize;
            _maxOrthoSize = Mathf.Max(size, _minOrthoSize + 10f);
            _orthoSize = _maxOrthoSize;
            if (hasBounds)
                _fallbackCityCenter = bounds.center;
        }

        private Vector3 ResolveOrigin()
        {
            if (_vehicle != null && _vehicle.IsDriving)
                return _vehicle.transform.position;

            _player = FindPlayer();
            if (_player != null)
                return _player.position;

            return _fallbackCityCenter;
        }

        private static Transform FindPlayer()
        {
            var gameManager = GameManager.Instance;
            if (gameManager == null || gameManager.userControl == null)
                return null;

            var localPlayer = gameManager.userControl.localPlayer;
            return localPlayer != null ? localPlayer.transform : null;
        }

        private void UpdateMarkers()
        {
            if (_overlayCanvas == null || _mapCamera == null)
                return;

            var origin = ResolveOrigin();
            var heading = 0f;
            if (_vehicle != null && _vehicle.IsDriving)
                heading = _vehicle.transform.eulerAngles.y;
            else if (_player != null)
                heading = _player.eulerAngles.y;

            PlaceMarker(_playerMarker, origin, true);
            _playerMarker.localRotation = Quaternion.Euler(0f, 0f, -heading);

            var targets = TargetHintTarget.All;
            var markerIndex = 0;
            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (target == null || !target.IsHintEnabled)
                    continue;

                var marker = GetTargetMarker(markerIndex);
                PlaceMarker(marker, target.transform.position, true);
                var image = marker.GetComponent<Image>();
                if (image != null)
                    image.color = GetKindColor(target.Kind);

                markerIndex++;
            }

            for (var i = markerIndex; i < _targetMarkers.Count; i++)
            {
                if (_targetMarkers[i].gameObject.activeSelf)
                    _targetMarkers[i].gameObject.SetActive(false);
            }
        }

        private void PlaceMarker(RectTransform marker, Vector3 world, bool active)
        {
            if (marker == null)
                return;

            var viewport = _mapCamera.WorldToViewportPoint(world);
            var visible = viewport.z > 0f
                && viewport.x > 0f && viewport.x < 1f
                && viewport.y > 0f && viewport.y < 1f;
            marker.gameObject.SetActive(active && visible);
            if (!marker.gameObject.activeSelf)
                return;

            var canvasRect = (RectTransform)_overlayCanvas.transform;
            marker.anchoredPosition = new Vector2(
                (viewport.x - 0.5f) * canvasRect.rect.width,
                (viewport.y - 0.5f) * canvasRect.rect.height);
        }

        private RectTransform GetTargetMarker(int index)
        {
            while (_targetMarkers.Count <= index)
                _targetMarkers.Add(CreateDot($"MapTarget_{_targetMarkers.Count + 1}", 18f, Color.white));

            var marker = _targetMarkers[index];
            if (!marker.gameObject.activeSelf)
                marker.gameObject.SetActive(true);

            return marker;
        }

        private static Color GetKindColor(TargetHintKind kind)
        {
            return kind switch
            {
                TargetHintKind.Vehicle => new Color(1f, 0.82f, 0.18f, 1f),
                TargetHintKind.Exit => new Color(0.2f, 0.92f, 0.42f, 1f),
                _ => new Color(1f, 0.15f, 0.12f, 1f)
            };
        }

        private void EnsureCamera()
        {
            if (_mapCamera != null)
                return;

            var go = new GameObject("CityMapCamera");
            go.transform.SetParent(transform, false);
            _mapCamera = go.AddComponent<Camera>();
            _mapCamera.orthographic = true;
            _mapCamera.orthographicSize = _fallbackCitySize;
            _mapCamera.nearClipPlane = 1f;
            _mapCamera.farClipPlane = 600f;
            _mapCamera.depth = 80f;
            _mapCamera.clearFlags = CameraClearFlags.SolidColor;
            _mapCamera.backgroundColor = new Color(0.08f, 0.1f, 0.12f, 1f);
            _mapCamera.enabled = false;
            _mapCamera.allowHDR = false;
            if (_mapCamera.GetComponent<AudioListener>() != null)
                Destroy(_mapCamera.GetComponent<AudioListener>());
        }

        private void EnsureOverlay()
        {
            if (_overlayCanvas != null)
                return;

            var canvasGo = new GameObject("CityMapOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            _overlayCanvas = canvasGo.GetComponent<Canvas>();
            _overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _overlayCanvas.sortingOrder = 80;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            CreateFrame();
            CreateHint();
            _playerMarker = CreatePlayerMarker();
        }

        private void CreateFrame()
        {
            var frame = new GameObject("MapFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            frame.transform.SetParent(_overlayCanvas.transform, false);
            var rect = (RectTransform)frame.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = frame.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.12f);
            image.raycastTarget = false;
        }

        private TextMeshProUGUI CreateHint()
        {
            var go = new GameObject("MapHint", typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(_overlayCanvas.transform, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -24f);
            rect.sizeDelta = new Vector2(900f, 48f);
            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = "Карта города  ·  M / N закрыть  ·  колёсико масштаб  ·  ПКМ двигать";
            label.fontSize = 28f;
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            label.raycastTarget = false;
            label.outlineWidth = 0.2f;
            label.outlineColor = new Color(0f, 0f, 0f, 0.75f);
            return label;
        }

        private RectTransform CreatePlayerMarker()
        {
            var go = new GameObject("MapPlayer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(_overlayCanvas.transform, false);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(28f, 28f);
            var image = go.GetComponent<Image>();
            image.sprite = CreateTriangleSprite();
            image.color = new Color(0.25f, 0.75f, 1f, 1f);
            image.raycastTarget = false;
            image.preserveAspect = true;
            return rect;
        }

        private RectTransform CreateDot(string objectName, float size, Color color)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(_overlayCanvas.transform, false);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(size, size);
            var image = go.GetComponent<Image>();
            image.sprite = CreateCircleSprite();
            image.color = color;
            image.raycastTarget = false;
            image.preserveAspect = true;
            return rect;
        }

        private static Sprite CreateTriangleSprite()
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "MapPlayerTriangle"
            };

            var clear = Color.clear;
            var fill = Color.white;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var nx = (x + 0.5f) / size;
                    var ny = (y + 0.5f) / size;
                    var halfWidth = ny * 0.5f;
                    var inside = ny > 0.08f && Mathf.Abs(nx - 0.5f) <= halfWidth * 0.9f;
                    texture.SetPixel(x, y, inside ? fill : clear);
                }
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite CreateCircleSprite()
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "MapTargetDot"
            };

            var center = (size - 1) * 0.5f;
            var radius = size * 0.42f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var inside = dx * dx + dy * dy <= radius * radius;
                    texture.SetPixel(x, y, inside ? Color.white : Color.clear);
                }
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
