using Game.Core.Mission;
using Game.Core.Passengers;
using Game.Core.Vehicles;
using HEAVYART.TopDownShooter.Netcode;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Core.UI
{
    [DisallowMultipleComponent]
    public sealed class GameplayHudController : MonoBehaviour
    {
        [SerializeField] private DriveableVehicleInteraction _vehicle;
        [SerializeField] private VehicleHealth _vehicleHealth;
        [SerializeField] private PassengerPickupSystem _pickup;
        [SerializeField] private LocationPressureSystem _pressure;

        private Image _playerFill;
        private Image _vehicleFill;
        private TextMeshProUGUI _timerLabel;
        private TextMeshProUGUI _passengerLabel;
        private TextMeshProUGUI _promptLabel;
        private GameObject _root;
        private GameObject _vehiclePanel;
        private Color _timerStart = Color.white;
        private Color _timerEnd = new(0.92f, 0.12f, 0.1f, 1f);

        public static GameplayHudController Ensure()
        {
            var existing = FindFirstObjectByType<GameplayHudController>(FindObjectsInactive.Include);
            if (existing != null)
                return existing;

            var canvas = GameObject.Find("InGameUI");
            var host = canvas != null ? canvas : new GameObject("GameplayHud");
            return host.GetComponent<GameplayHudController>() ?? host.AddComponent<GameplayHudController>();
        }

        private void Awake()
        {
            _vehicle ??= FindFirstObjectByType<DriveableVehicleInteraction>();
            _vehicleHealth ??= _vehicle != null ? _vehicle.GetComponent<VehicleHealth>() : FindFirstObjectByType<VehicleHealth>();
            _pickup ??= FindFirstObjectByType<PassengerPickupSystem>();
            _pressure ??= FindFirstObjectByType<LocationPressureSystem>();
            Build();
            HideLegacyHud();
        }

        private void Start()
        {
            if (_vehicleHealth == null && _vehicle != null)
                _vehicleHealth = _vehicle.GetComponent<VehicleHealth>();

            if (_vehicleHealth == null)
                return;

            _vehicleHealth.HealthChanged -= OnVehicleHealthChanged;
            _vehicleHealth.HealthChanged += OnVehicleHealthChanged;
            OnVehicleHealthChanged(_vehicleHealth.CurrentHealth, _vehicleHealth.MaxHealth);
        }

        private void OnEnable()
        {
            if (_pickup != null)
                _pickup.BoardedCountChanged += OnPassengersChanged;
            if (_vehicleHealth != null)
                _vehicleHealth.HealthChanged += OnVehicleHealthChanged;
        }

        private void OnDisable()
        {
            if (_pickup != null)
                _pickup.BoardedCountChanged -= OnPassengersChanged;
            if (_vehicleHealth != null)
                _vehicleHealth.HealthChanged -= OnVehicleHealthChanged;
        }

        private void Update()
        {
            UpdatePlayerHealth();
            UpdateTimer();
            UpdatePrompt();
            UpdateVehicleHudVisibility();
        }

        public void HideForEnding()
        {
            enabled = false;
            if (_root != null)
                _root.SetActive(false);
        }

        private void UpdatePlayerHealth()
        {
            if (_playerFill == null || GameManager.Instance == null || GameManager.Instance.userControl == null)
                return;

            var local = GameManager.Instance.userControl.localPlayer;
            if (local == null)
                return;

            var health = local.GetComponent<HealthController>();
            if (health == null || health.maxHealth <= 0.01f)
                return;

            _playerFill.fillAmount = Mathf.Clamp01(health.currentHealth / health.maxHealth);
        }

        private void UpdateTimer()
        {
            if (_timerLabel == null)
                return;

            if (_pressure == null)
                _pressure = LocationPressureSystem.Instance;

            var elapsed = _pressure != null ? _pressure.ElapsedSeconds : 0f;
            var progress = _pressure != null ? _pressure.Progress : 0f;
            var minutes = Mathf.FloorToInt(Mathf.Max(0f, elapsed) / 60f);
            var seconds = Mathf.FloorToInt(Mathf.Max(0f, elapsed) % 60f);
            _timerLabel.text = $"{minutes:00}:{seconds:00}";
            _timerLabel.color = Color.Lerp(_timerStart, _timerEnd, progress);
        }

        private void UpdatePrompt()
        {
            if (_promptLabel == null || _vehicle == null)
                return;

            _promptLabel.text = _vehicle.IsDriving
                ? "E  —  ВЫЙТИ ИЗ МАШИНЫ"
                : _vehicle.CanEnter
                    ? "E  —  СЕСТЬ В МАШИНУ"
                    : string.Empty;
        }

        private void OnPassengersChanged(int boarded, int max)
        {
            if (_passengerLabel != null)
                _passengerLabel.text = $"ПАССАЖИРЫ  {boarded}/{max}";
        }

        private void OnVehicleHealthChanged(float current, float max)
        {
            if (_vehicleFill != null && max > 0.01f)
                _vehicleFill.fillAmount = Mathf.Clamp01(current / max);
        }

        private void HideLegacyHud()
        {
            var timer = transform.Find("LocationTimer");
            if (timer != null)
                timer.gameObject.SetActive(false);

            var counter = GameObject.Find("PassengerCounter");
            if (counter != null)
                counter.SetActive(false);

            var hud = FindFirstObjectByType<HUDController>(FindObjectsInactive.Include);
            if (hud == null)
                return;

            if (hud.healthStatusSlider != null)
                hud.healthStatusSlider.gameObject.SetActive(false);
            if (hud.gameTimerTextComponent != null)
                hud.gameTimerTextComponent.gameObject.SetActive(false);
        }

        private void Build()
        {
            if (_root != null)
                return;

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                var inGameUi = GameObject.Find("InGameUI");
                canvas = inGameUi != null ? inGameUi.GetComponent<Canvas>() : FindFirstObjectByType<Canvas>();
            }

            var parent = canvas != null ? canvas.transform : transform;
            _root = CreateUi("GameplayHudRoot", parent);
            StretchFull(_root.GetComponent<RectTransform>());
            var group = _root.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            var fontTitle = GameplayUiStyle.LoadTitleFont();
            var fontBody = GameplayUiStyle.LoadBodyFont();
            var frame = GameplayUiStyle.LoadFrame();
            var barEmpty = GameplayUiStyle.LoadBarEmpty();
            var barFull = GameplayUiStyle.LoadBarFull();

            var playerPanel = CreateFramedPanel("PlayerStatus", _root.transform, frame, new Vector2(0f, 1f), new Vector2(28f, -24f), new Vector2(360f, 92f));
            CreateLabel("PlayerLabel", playerPanel, "ВЫЖИВШИЙ", fontTitle, 18f, GameplayUiStyle.TextDim, new Vector2(16f, -8f), new Vector2(200f, 24f), TextAlignmentOptions.Left);
            _playerFill = CreateBar("PlayerHealth", playerPanel, barEmpty, barFull, new Vector2(16f, -38f), new Vector2(328f, 28f));
            _timerLabel = CreateLabel("Timer", playerPanel, "00:00", fontBody, 22f, GameplayUiStyle.TextLight, new Vector2(250f, -8f), new Vector2(94f, 24f), TextAlignmentOptions.Right);

            var passengerPanel = CreateFramedPanel("PassengerStatus", _root.transform, frame, new Vector2(1f, 1f), new Vector2(-28f, -24f), new Vector2(340f, 72f));
            var passengerRt = passengerPanel.GetComponent<RectTransform>();
            passengerRt.pivot = new Vector2(1f, 1f);
            _passengerLabel = CreateLabel("PassengerLabel", passengerPanel, "ПАССАЖИРЫ  0/5", fontTitle, 26f, GameplayUiStyle.TextLight, Vector2.zero, new Vector2(340f, 72f), TextAlignmentOptions.Center);
            _passengerLabel.rectTransform.anchorMin = Vector2.zero;
            _passengerLabel.rectTransform.anchorMax = Vector2.one;
            _passengerLabel.rectTransform.offsetMin = Vector2.zero;
            _passengerLabel.rectTransform.offsetMax = Vector2.zero;

            var vehiclePanel = CreateFramedPanel("VehicleStatus", _root.transform, frame, new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(520f, 86f));
            _vehiclePanel = vehiclePanel;
            var vehicleRt = vehiclePanel.GetComponent<RectTransform>();
            vehicleRt.pivot = new Vector2(0.5f, 0f);
            CreateLabel("VehicleLabel", vehiclePanel, "ГРУЗОВИК", fontTitle, 18f, GameplayUiStyle.TextDim, new Vector2(16f, -8f), new Vector2(488f, 24f), TextAlignmentOptions.Center);
            _vehicleFill = CreateBar("VehicleHealth", vehiclePanel, barEmpty, barFull, new Vector2(16f, -36f), new Vector2(488f, 28f));
            if (_vehicleHealth != null)
                OnVehicleHealthChanged(_vehicleHealth.CurrentHealth, _vehicleHealth.MaxHealth);

            _promptLabel = CreateLabel("Prompt", _root, string.Empty, fontBody, 22f, GameplayUiStyle.TextLight, new Vector2(0f, 126f), new Vector2(520f, 36f), TextAlignmentOptions.Center);
            var promptRt = _promptLabel.rectTransform;
            promptRt.anchorMin = new Vector2(0.5f, 0f);
            promptRt.anchorMax = new Vector2(0.5f, 0f);
            promptRt.pivot = new Vector2(0.5f, 0f);

            if (_pickup != null)
                OnPassengersChanged(_pickup.BoardedCount, _pickup.MaxCount);

            UpdateVehicleHudVisibility();
        }

        private void UpdateVehicleHudVisibility()
        {
            if (_vehiclePanel == null)
                return;

            var visible = _vehicle != null && _vehicle.IsDriving;
            if (_vehiclePanel.activeSelf != visible)
                _vehiclePanel.SetActive(visible);
        }

        private static GameObject CreateFramedPanel(string name, Transform parent, Sprite frame, Vector2 anchor, Vector2 anchored, Vector2 size)
        {
            var go = CreateUi(name, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchored;
            rt.sizeDelta = size;

            var bg = go.AddComponent<Image>();
            bg.color = GameplayUiStyle.PanelBg;
            bg.raycastTarget = false;

            if (frame != null)
            {
                var frameGo = CreateUi("Frame", go.transform);
                StretchFull(frameGo.GetComponent<RectTransform>());
                var image = frameGo.AddComponent<Image>();
                image.sprite = frame;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
                image.raycastTarget = false;
            }

            return go;
        }

        private static Image CreateBar(string name, GameObject parent, Sprite empty, Sprite full, Vector2 anchored, Vector2 size)
        {
            var track = CreateUi(name, parent.transform);
            var trackRt = track.GetComponent<RectTransform>();
            trackRt.anchorMin = new Vector2(0f, 1f);
            trackRt.anchorMax = new Vector2(0f, 1f);
            trackRt.pivot = new Vector2(0f, 1f);
            trackRt.anchoredPosition = anchored;
            trackRt.sizeDelta = size;

            var trackImage = track.AddComponent<Image>();
            trackImage.sprite = empty;
            trackImage.type = Image.Type.Sliced;
            trackImage.color = empty != null ? Color.white : new Color(0.12f, 0.12f, 0.12f, 0.9f);
            trackImage.raycastTarget = false;

            var fillGo = CreateUi("Fill", track.transform);
            StretchFull(fillGo.GetComponent<RectTransform>());
            var fill = fillGo.AddComponent<Image>();
            fill.sprite = full;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.color = full != null ? Color.white : GameplayUiStyle.AccentRed;
            fill.fillAmount = 1f;
            fill.raycastTarget = false;
            return fill;
        }

        private static TextMeshProUGUI CreateLabel(
            string name,
            GameObject parent,
            string text,
            TMP_FontAsset font,
            float size,
            Color color,
            Vector2 anchored,
            Vector2 sizeDelta,
            TextAlignmentOptions align)
        {
            var go = CreateUi(name, parent.transform);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchored;
            rt.sizeDelta = sizeDelta;
            var label = go.AddComponent<TextMeshProUGUI>();
            if (font != null)
                label.font = font;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = align;
            label.raycastTarget = false;
            label.enableWordWrapping = false;
            return label;
        }

        private static GameObject CreateUi(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
