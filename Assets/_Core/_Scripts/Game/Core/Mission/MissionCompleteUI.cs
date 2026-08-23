using System;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Core.Mission
{
    /// <summary>
    /// Confirmation dialog and escape ending overlay.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MissionCompleteUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _confirmGroup;
        [SerializeField] private CanvasGroup _endingGroup;
        [SerializeField] private TextMeshProUGUI _endingLabel;
        [SerializeField] private string _endingText = "Мы выбрались";

        private Tween _confirmTween;
        private Tween _endingTween;
        private Action _onConfirm;
        private Action _onCancel;
        private CursorLockMode _savedCursorLock;
        private bool _savedCursorVisible;

        public bool IsConfirmVisible =>
            _confirmGroup != null && _confirmGroup.gameObject.activeSelf && _confirmGroup.blocksRaycasts;

        private void Awake()
        {
            EnsureUi();
            HideImmediate();
        }

        private void OnDestroy()
        {
            _confirmTween.Stop();
            _endingTween.Stop();
        }

        public void ShowConfirm(Action onConfirm, Action onCancel)
        {
            EnsureUi();
            _onConfirm = onConfirm;
            _onCancel = onCancel;

            _endingGroup.alpha = 0f;
            _endingGroup.blocksRaycasts = false;
            _endingGroup.interactable = false;
            _endingGroup.gameObject.SetActive(false);

            _confirmGroup.gameObject.SetActive(true);
            _confirmGroup.transform.SetAsLastSibling();
            BindConfirmButtons();
            SetCursorForUi(true);
            _confirmTween.Stop();
            _confirmTween = Tween.Alpha(_confirmGroup, endValue: 1f, duration: 0.25f, ease: Ease.OutSine);
            _confirmGroup.blocksRaycasts = true;
            _confirmGroup.interactable = true;
        }

        public void HideConfirm()
        {
            if (_confirmGroup == null)
                return;

            _confirmTween.Stop();
            _confirmTween = Tween.Alpha(_confirmGroup, endValue: 0f, duration: 0.2f, ease: Ease.OutSine);
            _confirmGroup.blocksRaycasts = false;
            _confirmGroup.interactable = false;
            SetCursorForUi(false);
        }

        public void PlayEndingOverlay()
        {
            EnsureUi();
            HideConfirm();
            _endingGroup.gameObject.SetActive(true);
            if (_endingLabel != null)
                _endingLabel.text = _endingText;

            _endingTween.Stop();
            _endingGroup.alpha = 0f;
            _endingTween = Tween.Alpha(_endingGroup, endValue: 1f, duration: 1.4f, ease: Ease.InOutSine);
            _endingGroup.blocksRaycasts = true;
            _endingGroup.interactable = false;
        }

        private void Update()
        {
            if (!IsConfirmVisible)
                return;

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Y))
                OnYesClicked();
            else if (Input.GetKeyDown(KeyCode.Escape))
                OnNoClicked();
        }

        private void HideImmediate()
        {
            if (_confirmGroup != null)
            {
                _confirmGroup.alpha = 0f;
                _confirmGroup.blocksRaycasts = false;
                _confirmGroup.interactable = false;
                _confirmGroup.gameObject.SetActive(false);
            }

            if (_endingGroup != null)
            {
                _endingGroup.alpha = 0f;
                _endingGroup.blocksRaycasts = false;
                _endingGroup.interactable = false;
                _endingGroup.gameObject.SetActive(false);
            }
        }

        private void OnYesClicked()
        {
            HideConfirm();
            _onConfirm?.Invoke();
        }

        private void OnNoClicked()
        {
            HideConfirm();
            _onCancel?.Invoke();
        }

        private void EnsureUi()
        {
            if (_confirmGroup != null && _endingGroup != null)
                return;

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = FindFirstObjectByType<Canvas>();

            var parent = canvas != null ? canvas.transform : transform;

            if (_confirmGroup == null)
                _confirmGroup = BuildConfirm(parent);

            if (_endingGroup == null)
                _endingGroup = BuildEnding(parent);

            BindConfirmButtons();
        }

        private void BindConfirmButtons()
        {
            if (_confirmGroup == null)
                return;

            var yes = FindButton(_confirmGroup.transform, "YesButton");
            var no = FindButton(_confirmGroup.transform, "NoButton");
            if (yes != null)
            {
                yes.onClick.RemoveListener(OnYesClicked);
                yes.onClick.AddListener(OnYesClicked);
            }

            if (no != null)
            {
                no.onClick.RemoveListener(OnNoClicked);
                no.onClick.AddListener(OnNoClicked);
            }
        }

        private void SetCursorForUi(bool uiActive)
        {
            if (uiActive)
            {
                _savedCursorLock = Cursor.lockState;
                _savedCursorVisible = Cursor.visible;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            Cursor.lockState = _savedCursorLock;
            Cursor.visible = _savedCursorVisible;
        }

        private static Button FindButton(Transform root, string name)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name != name)
                    continue;

                var button = transforms[i].GetComponent<Button>();
                if (button != null)
                    return button;
            }

            return null;
        }

        private CanvasGroup BuildConfirm(Transform parent)
        {
            var root = CreateFullScreen("MissionConfirm", parent, new Color(0f, 0f, 0f, 0.62f));
            var group = root.gameObject.AddComponent<CanvasGroup>();

            var panel = CreatePanel("Panel", root, new Vector2(560f, 280f), new Color(0.08f, 0.08f, 0.1f, 0.96f));
            CreateLabel("Title", panel, "Завершить побег?", 36f, new Vector2(0f, 70f), new Vector2(500f, 80f));
            CreateLabel("Body", panel, "Вы у выхода из города. Хотите завершить игру?", 22f, new Vector2(0f, 8f), new Vector2(500f, 70f));

            CreateButton("YesButton", panel, "Да", new Vector2(-120f, -80f), new Color(0.18f, 0.62f, 0.28f, 1f));
            CreateButton("NoButton", panel, "Нет", new Vector2(120f, -80f), new Color(0.55f, 0.18f, 0.18f, 1f));
            return group;
        }

        private CanvasGroup BuildEnding(Transform parent)
        {
            var root = CreateFullScreen("MissionEnding", parent, new Color(0f, 0f, 0f, 0.88f));
            var group = root.gameObject.AddComponent<CanvasGroup>();
            _endingLabel = CreateLabel("EndingText", root, _endingText, 52f, Vector2.zero, new Vector2(900f, 160f));
            return group;
        }

        private static RectTransform CreateFullScreen(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            go.transform.SetAsLastSibling();
            return rect;
        }

        private static RectTransform CreatePanel(string name, Transform parent, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = color;
            return rect;
        }

        private static TextMeshProUGUI CreateLabel(string name, Transform parent, string text, float size, Vector2 anchored, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchored;
            rect.sizeDelta = sizeDelta;
            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        private static Button CreateButton(string name, Transform parent, string text, Vector2 anchored, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchored;
            rect.sizeDelta = new Vector2(180f, 56f);
            var image = go.GetComponent<Image>();
            image.color = color;
            CreateLabel("Label", go.transform, text, 26f, Vector2.zero, new Vector2(180f, 56f));
            return go.GetComponent<Button>();
        }
    }
}
