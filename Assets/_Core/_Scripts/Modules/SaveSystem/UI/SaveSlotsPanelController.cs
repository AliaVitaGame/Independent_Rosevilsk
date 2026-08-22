using System;
using Modules.SaveSystem;
using Modules.SaveSystem.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.SaveSystem.UI
{
    public enum SaveSlotsPanelMode
    {
        NewGame,
        LoadGame
    }

    /// <summary>
    /// Bloodlines-styled 3-slot panel with overwrite confirmation.
    /// Builds its own UI tree if empty.
    /// </summary>
    public sealed class SaveSlotsPanelController : MonoBehaviour
    {
        private static readonly Color AccentRed = new(0.72f, 0.08f, 0.08f, 1f);
        private static readonly Color PanelBg = new(0.05f, 0.05f, 0.06f, 0.96f);
        private static readonly Color OverlayBg = new(0f, 0f, 0f, 0.72f);
        private static readonly Color TextLight = new(0.9f, 0.9f, 0.88f, 1f);
        private static readonly Color TextDim = new(0.6f, 0.6f, 0.58f, 1f);

        public event Action<int> SlotChosen;

        public SaveSlotsPanelMode CurrentMode => _mode;

        private IGameSaveService _saveService;
        private SaveSlotsPanelMode _mode;
        private TextMeshProUGUI _title;
        private readonly SlotView[] _slots = new SlotView[3];
        private GameObject _confirmRoot;
        private TextMeshProUGUI _confirmText;
        private int _pendingOverwriteSlot = -1;

        private sealed class SlotView
        {
            public Button Button;
            public TextMeshProUGUI Title;
            public TextMeshProUGUI Details;
            public Image Frame;
        }

        public void Initialize(IGameSaveService saveService)
        {
            _saveService = saveService;
            EnsureUi();
            Hide();
        }

        public void Show(SaveSlotsPanelMode mode)
        {
            _mode = mode;
            EnsureUi();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            if (_title != null)
            {
                _title.text = mode == SaveSlotsPanelMode.NewGame
                    ? "NEW GAME — SELECT SLOT"
                    : "LOAD GAME — SELECT SLOT";
            }

            HideConfirm();
            RefreshSlots();
        }

        public void Hide()
        {
            HideConfirm();
            gameObject.SetActive(false);
        }

        private void RefreshSlots()
        {
            if (_saveService == null)
                return;

            var slots = _saveService.GetSlots();
            for (var i = 0; i < _slots.Length; i++)
            {
                var view = _slots[i];
                if (view == null)
                    continue;

                var info = i < slots.Count ? slots[i] : new SaveSlotInfo { slotIndex = i, isEmpty = true };
                var occupied = !info.isEmpty;

                view.Title.text = $"SLOT {i + 1}";
                if (occupied)
                {
                    view.Details.text = $"{info.FormatSavedAtLocal()}   •   {info.FormatPlaytime()}";
                    view.Details.color = TextLight;
                }
                else
                {
                    view.Details.text = "EMPTY";
                    view.Details.color = TextDim;
                }

                var interactable = _mode == SaveSlotsPanelMode.NewGame || occupied;
                view.Button.interactable = interactable;
                view.Frame.color = interactable ? new Color(1f, 1f, 1f, 0.85f) : new Color(1f, 1f, 1f, 0.25f);
            }
        }

        private void OnSlotClicked(int slotIndex)
        {
            if (_saveService == null)
                return;

            var info = _saveService.GetSlot(slotIndex);

            if (_mode == SaveSlotsPanelMode.NewGame && !info.isEmpty)
            {
                ShowConfirm(slotIndex);
                return;
            }

            if (_mode == SaveSlotsPanelMode.LoadGame && info.isEmpty)
                return;

            SlotChosen?.Invoke(slotIndex);
            Hide();
        }

        private void ShowConfirm(int slotIndex)
        {
            _pendingOverwriteSlot = slotIndex;
            if (_confirmRoot != null)
                _confirmRoot.SetActive(true);

            if (_confirmText != null)
                _confirmText.text = $"Overwrite Slot {slotIndex + 1}?\nAll progress in this slot will be lost.";
        }

        private void HideConfirm()
        {
            _pendingOverwriteSlot = -1;
            if (_confirmRoot != null)
                _confirmRoot.SetActive(false);
        }

        private void ConfirmOverwrite()
        {
            var slot = _pendingOverwriteSlot;
            HideConfirm();
            if (slot < 0)
                return;

            SlotChosen?.Invoke(slot);
            Hide();
        }

        private void EnsureUi()
        {
            if (_title != null)
                return;

            var rootRt = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            Stretch(rootRt);

            var overlay = CreatePanel("Overlay", transform, OverlayBg);
            Stretch(overlay.GetComponent<RectTransform>());

            var panel = CreatePanel("Panel", transform, PanelBg);
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(720f, 560f);

            _title = CreateTmp("Title", panel.transform, "SELECT SLOT", 36f, AccentRed);
            var titleRt = _title.rectTransform;
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -28f);
            titleRt.sizeDelta = new Vector2(-48f, 48f);
            _title.alignment = TextAlignmentOptions.Center;

            for (var i = 0; i < 3; i++)
                _slots[i] = CreateSlotRow(panel.transform, i);

            var close = CreateButton("CloseButton", panel.transform, "CLOSE", TextDim);
            var closeRt = close.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.5f, 0f);
            closeRt.anchorMax = new Vector2(0.5f, 0f);
            closeRt.pivot = new Vector2(0.5f, 0f);
            closeRt.anchoredPosition = new Vector2(0f, 24f);
            closeRt.sizeDelta = new Vector2(180f, 44f);
            close.onClick.AddListener(Hide);

            BuildConfirm(panel.transform);
            ApplyBloodlinesLook();
        }

        private void ApplyBloodlinesLook()
        {
            var titleFont = FindLoadedFont("ManufacturingConsent") ?? FindLoadedFont("MedievalSharp");
            var bodyFont = FindLoadedFont("Roboto-Bold") ?? FindLoadedFont("Roboto");
            var frameSprite = FindLoadedSprite("Frame_outline_red") ?? FindLoadedSprite("Frame_outline");

            if (titleFont != null && _title != null)
            {
                _title.font = titleFont;
                _title.fontStyle = FontStyles.UpperCase;
            }

            if (bodyFont != null)
            {
                foreach (var slot in _slots)
                {
                    if (slot?.Title != null)
                        slot.Title.font = bodyFont;
                    if (slot?.Details != null)
                        slot.Details.font = bodyFont;
                }

                ApplyFontRecursive(_confirmRoot != null ? _confirmRoot.transform : null, bodyFont);
                var closeLabel = transform.Find("Panel/CloseButton/Label")?.GetComponent<TextMeshProUGUI>();
                if (closeLabel != null)
                    closeLabel.font = bodyFont;
            }

            if (frameSprite == null)
                return;

            foreach (var slot in _slots)
            {
                if (slot?.Frame == null)
                    continue;

                slot.Frame.sprite = frameSprite;
                slot.Frame.type = Image.Type.Sliced;
                slot.Frame.color = Color.white;
            }
        }

        private static void ApplyFontRecursive(Transform root, TMP_FontAsset font)
        {
            if (root == null || font == null)
                return;

            foreach (var tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                tmp.font = font;
        }

        private static TMP_FontAsset FindLoadedFont(string nameContains)
        {
            var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            foreach (var font in fonts)
            {
                if (font != null && font.name.IndexOf(nameContains, StringComparison.OrdinalIgnoreCase) >= 0)
                    return font;
            }

            return null;
        }

        private static Sprite FindLoadedSprite(string nameContains)
        {
            var sprites = Resources.FindObjectsOfTypeAll<Sprite>();
            foreach (var sprite in sprites)
            {
                if (sprite != null && sprite.name.IndexOf(nameContains, StringComparison.OrdinalIgnoreCase) >= 0)
                    return sprite;
            }

            return null;
        }

        private SlotView CreateSlotRow(Transform parent, int index)
        {
            var row = CreatePanel($"Slot{index}", parent, new Color(0.09f, 0.09f, 0.1f, 1f));
            var rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -100f - index * 110f);
            rt.sizeDelta = new Vector2(620f, 92f);

            var frame = row.AddComponent<Outline>();
            frame.effectColor = AccentRed;
            frame.effectDistance = new Vector2(1.5f, 1.5f);

            var button = row.AddComponent<Button>();
            button.targetGraphic = row.GetComponent<Image>();
            var captured = index;
            button.onClick.AddListener(() => OnSlotClicked(captured));

            var title = CreateTmp("SlotTitle", row.transform, $"SLOT {index + 1}", 26f, TextLight);
            var titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0f, 0.5f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = new Vector2(24f, 0f);
            titleRt.offsetMax = new Vector2(-24f, -10f);
            title.alignment = TextAlignmentOptions.BottomLeft;

            var details = CreateTmp("SlotDetails", row.transform, "EMPTY", 20f, TextDim);
            var detailsRt = details.rectTransform;
            detailsRt.anchorMin = new Vector2(0f, 0f);
            detailsRt.anchorMax = new Vector2(1f, 0.5f);
            detailsRt.offsetMin = new Vector2(24f, 12f);
            detailsRt.offsetMax = new Vector2(-24f, 0f);
            details.alignment = TextAlignmentOptions.TopLeft;

            return new SlotView
            {
                Button = button,
                Title = title,
                Details = details,
                Frame = row.GetComponent<Image>()
            };
        }

        private void BuildConfirm(Transform parent)
        {
            _confirmRoot = CreatePanel("OverwriteConfirm", parent, new Color(0.08f, 0.04f, 0.04f, 0.98f));
            var rt = _confirmRoot.GetComponent<RectTransform>();
            Stretch(rt);
            _confirmRoot.SetActive(false);

            _confirmText = CreateTmp("ConfirmText", _confirmRoot.transform, "Overwrite?", 28f, TextLight);
            var textRt = _confirmText.rectTransform;
            textRt.anchorMin = new Vector2(0.1f, 0.45f);
            textRt.anchorMax = new Vector2(0.9f, 0.75f);
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            _confirmText.alignment = TextAlignmentOptions.Center;

            var yes = CreateButton("YesButton", _confirmRoot.transform, "OVERWRITE", AccentRed);
            var yesRt = yes.GetComponent<RectTransform>();
            yesRt.anchorMin = new Vector2(0.5f, 0.22f);
            yesRt.anchorMax = new Vector2(0.5f, 0.22f);
            yesRt.sizeDelta = new Vector2(200f, 48f);
            yesRt.anchoredPosition = new Vector2(-120f, 0f);
            yes.onClick.AddListener(ConfirmOverwrite);

            var no = CreateButton("NoButton", _confirmRoot.transform, "CANCEL", TextDim);
            var noRt = no.GetComponent<RectTransform>();
            noRt.anchorMin = new Vector2(0.5f, 0.22f);
            noRt.anchorMax = new Vector2(0.5f, 0.22f);
            noRt.sizeDelta = new Vector2(200f, 48f);
            noRt.anchoredPosition = new Vector2(120f, 0f);
            no.onClick.AddListener(HideConfirm);
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static Button CreateButton(string name, Transform parent, string label, Color labelColor)
        {
            var go = CreatePanel(name, parent, new Color(0.12f, 0.12f, 0.13f, 1f));
            var button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            var tmp = CreateTmp("Label", go.transform, label, 22f, labelColor);
            Stretch(tmp.rectTransform);
            tmp.alignment = TextAlignmentOptions.Center;
            return button;
        }

        private static TextMeshProUGUI CreateTmp(string name, Transform parent, string text, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
