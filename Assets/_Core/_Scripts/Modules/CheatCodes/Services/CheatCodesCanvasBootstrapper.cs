using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using R3;
using VContainer;
using UnityEngine.InputSystem;

namespace GameTest
{
    public sealed class CheatCodesCanvasBootstrapper : ICheatCodesRuntimeUi, IDisposable
    {
        private readonly ICheatCodeRegistry _registry;
        private readonly IObjectResolver _resolver;
        private readonly CompositeDisposable _disposables = new();

        private GameObject _root;
        private MethodButtonGenerator _generator;
        private bool _isVisible;
        private bool _isInitialized;

        public CheatCodesCanvasBootstrapper(ICheatCodeRegistry registry, IObjectResolver resolver)
        {
            _registry = registry;
            _resolver = resolver;
        }

        public void Initialize()
        {
            if (_isInitialized || _registry == null)
                return;

            _registry.WarmUp();
            CreateCanvas();

            var targets = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            _generator.GenerateMethodButtons(_registry, targets, ResolveFromDi);
            if (_registry.Categories.Count == 0)
                Debug.LogWarning("[CheatCode] Registry is empty after WarmUp. No cheat buttons were generated.");
            SetVisible(false);
            SubscribeToggle();
            _isInitialized = true;
        }

        public void Dispose()
        {
            _disposables.Dispose();
            if (_root != null)
                UnityEngine.Object.Destroy(_root);
        }

        private void CreateCanvas()
        {
            _root = new GameObject("CheatCodesRuntimeUI");
            UnityEngine.Object.DontDestroyOnLoad(_root);

            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10_000;

            var scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _root.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();

            var panel = CreatePanel(_root.transform);
            var content = CreateScrollContent(panel.transform);
            var buttonTemplate = CreateButtonTemplate(content);

            _generator = panel.gameObject.AddComponent<MethodButtonGenerator>();
            _generator.Configure(buttonTemplate.gameObject, null, content);
        }

        private static RectTransform CreatePanel(Transform parent)
        {
            var panel = new GameObject("Scroll View", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            panel.transform.SetParent(parent, false);

            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(200f, 0f);
            rect.sizeDelta = new Vector2(400f, 0f);

            var image = panel.GetComponent<Image>();
            image.type = Image.Type.Simple;
            image.color = new Color(0f, 0f, 0f, 0.392f);

            return rect;
        }

        private static Transform CreateScrollContent(Transform parent)
        {
            var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportObject.transform.SetParent(parent, false);

            var viewportRect = viewportObject.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewportRect.pivot = new Vector2(0f, 1f);

            var viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.type = Image.Type.Simple;
            viewportImage.color = Color.white;

            var mask = viewportObject.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            var content = new GameObject("Buttons", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewportObject.transform, false);

            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(25, 25, 25, 25);
            layout.spacing = 10f;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;

            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var scrollRect = parent.GetComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 30f;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

            return content.transform;
        }

        private static RectTransform CreateButtonTemplate(Transform parent)
        {
            var buttonObject = new GameObject("ButtonTemplate", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.SetActive(false);

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 34f);

            var image = buttonObject.GetComponent<Image>();
            image.type = Image.Type.Simple;
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.98f);

            var layoutElement = buttonObject.GetComponent<LayoutElement>();
            layoutElement.minHeight = 34f;
            layoutElement.preferredHeight = 34f;
            layoutElement.flexibleWidth = 1f;

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);

            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 4f);
            labelRect.offsetMax = new Vector2(-8f, -4f);

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.font = TMP_Settings.defaultFontAsset;
            label.text = "Cheat";
            label.fontSize = 20f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.MidlineLeft;

            return rect;
        }

        private static void EnsureEventSystem()
        {
            var eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem != null)
                return;

            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            UnityEngine.Object.DontDestroyOnLoad(eventSystemObject);
        }

        private object ResolveFromDi(Type type)
        {
            if (_resolver == null)
                return null;

            return _resolver.TryResolve(type, out var resolved) ? resolved : null;
        }

        private void SetVisible(bool isVisible)
        {
            _isVisible = isVisible;
            if (_root != null)
                _root.SetActive(_isVisible);
        }

        private void SubscribeToggle()
        {
            Observable.EveryUpdate()
                .Where(_ => _isInitialized && Keyboard.current?.backquoteKey.wasPressedThisFrame == true)
                .Subscribe(_ => SetVisible(!_isVisible))
                .AddTo(_disposables);
        }
    }
}
