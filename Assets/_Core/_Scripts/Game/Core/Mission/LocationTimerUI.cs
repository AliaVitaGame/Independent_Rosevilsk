using TMPro;
using UnityEngine;

namespace Game.Core.Mission
{
    /// <summary>
    /// Left-side elapsed timer that shifts from white to red over five minutes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LocationTimerUI : MonoBehaviour
    {
        [SerializeField] private LocationPressureSystem _pressure;
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private Color _startColor = Color.white;
        [SerializeField] private Color _endColor = new(0.92f, 0.12f, 0.1f, 1f);

        private void Awake()
        {
            if (_pressure == null)
                _pressure = FindFirstObjectByType<LocationPressureSystem>();

            EnsureLabel();
        }

        private void Update()
        {
            if (_label == null)
                return;

            if (_pressure == null)
                _pressure = LocationPressureSystem.Instance;

            var elapsed = _pressure != null ? _pressure.ElapsedSeconds : 0f;
            var progress = _pressure != null ? _pressure.Progress : 0f;
            var clamped = Mathf.Max(0f, elapsed);
            var minutes = Mathf.FloorToInt(clamped / 60f);
            var seconds = Mathf.FloorToInt(clamped % 60f);
            _label.text = $"{minutes:00}:{seconds:00}";
            _label.color = Color.Lerp(_startColor, _endColor, progress);
        }

        private void EnsureLabel()
        {
            if (_label != null)
                return;

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                var inGameUi = GameObject.Find("InGameUI");
                canvas = inGameUi != null ? inGameUi.GetComponent<Canvas>() : FindFirstObjectByType<Canvas>();
            }

            var parent = canvas != null ? canvas.transform : transform;
            var go = new GameObject("LocationTimer", typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(28f, 80f);
            rect.sizeDelta = new Vector2(240f, 72f);

            _label = go.AddComponent<TextMeshProUGUI>();
            _label.fontSize = 44f;
            _label.alignment = TextAlignmentOptions.Left;
            _label.fontStyle = FontStyles.Bold;
            _label.color = _startColor;
            _label.raycastTarget = false;
            _label.outlineWidth = 0.18f;
            _label.outlineColor = new Color(0f, 0f, 0f, 0.7f);
            _label.text = "00:00";
        }

        public void HideForEnding()
        {
            enabled = false;
            if (_label != null)
                _label.gameObject.SetActive(false);
        }
    }
}
