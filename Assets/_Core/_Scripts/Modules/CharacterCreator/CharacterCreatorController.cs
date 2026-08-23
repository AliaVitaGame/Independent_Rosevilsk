using System;
using CC;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.CharacterCreator
{
    /// <summary>
    /// Hosts CharacterCustomizer characters + UI inside MainMenu.
    /// Shown after a New Game slot is chosen.
    /// </summary>
    public sealed class CharacterCreatorController : MonoBehaviour
    {
        private const string CharactersPrefabPath =
            "Assets/CharacterCustomizer/Characters/Prefabs/CustomizableCharacters.prefab";
        private const string UiPrefabPath =
            "Assets/CharacterCustomizer/UI/Prefabs/UIs/UI.prefab";
        private const string HalfSpherePrefabPath =
            "Assets/CharacterCustomizer/Environment/HalfSphere/HalfSphere.prefab";

        private static readonly Color AccentRed = new(0.72f, 0.08f, 0.08f, 1f);
        private static readonly Color TextDim = new(0.6f, 0.6f, 0.58f, 1f);

        // Prefer human body over Dummy for new-game flow.
        private const int DefaultCharacterIndex = 2; // Male_Combined_HP

        public event Action<int, CC_CharacterData> Confirmed;
        public event Action Cancelled;

        [SerializeField] private GameObject charactersRoot;
        [SerializeField] private GameObject uiRoot;
        [SerializeField] private GameObject environmentRoot;
        [SerializeField] private Camera creatorCamera;
        [SerializeField] private Canvas overlayCanvas;

        private GameObject _menuRoot;
        private GameObject _menuBackground;
        private Camera _menuCamera;
        private bool _menuCameraWasEnabled;
        private bool _menuBackgroundWasActive;
        private Vector3 _menuCamPos;
        private Quaternion _menuCamRot;
        private float _menuCamFov;
        private bool _built;
        private bool _visible;
        private bool _wired;
        private Coroutine _applyDefaultRoutine;
        private int _pendingNewGameSlot = -1;

        public bool IsVisible => _visible;

        public void ShowForNewGame(int slotIndex)
        {
            _pendingNewGameSlot = slotIndex;
            Show();
        }

        public void Show()
        {
            EnsureBuilt();
            if (!_built)
                return;

            gameObject.SetActive(true);
            CacheAndHideMenu();
            ApplyCreatorCamera();

            if (environmentRoot != null)
                environmentRoot.SetActive(true);
            if (charactersRoot != null)
                charactersRoot.SetActive(true);
            if (uiRoot != null)
            {
                uiRoot.SetActive(true);
                BoostUiCanvasSorting();
            }

            EnsureOverlay();
            if (overlayCanvas != null)
                overlayCanvas.gameObject.SetActive(true);

            _visible = true;
            ApplyDefaultCharacterNow();

            if (_applyDefaultRoutine != null)
                StopCoroutine(_applyDefaultRoutine);
            _applyDefaultRoutine = StartCoroutine(ApplyDefaultCharacterAfterUiStart());
        }

        public void Hide()
        {
            _visible = false;
            _pendingNewGameSlot = -1;

            if (_applyDefaultRoutine != null)
            {
                StopCoroutine(_applyDefaultRoutine);
                _applyDefaultRoutine = null;
            }

            if (overlayCanvas != null)
                overlayCanvas.gameObject.SetActive(false);
            if (uiRoot != null)
                uiRoot.SetActive(false);
            if (charactersRoot != null)
                charactersRoot.SetActive(false);
            if (environmentRoot != null)
                environmentRoot.SetActive(false);

            RestoreMenu();
            gameObject.SetActive(false);
        }

        private System.Collections.IEnumerator ApplyDefaultCharacterAfterUiStart()
        {
            // CC_UI_Manager.Start runs after first activation and used to force Dummy.
            yield return null;
            ApplyDefaultCharacterNow();
            BoostUiCanvasSorting();
            _applyDefaultRoutine = null;
        }

        private void ApplyDefaultCharacterNow()
        {
            var manager = uiRoot != null
                ? uiRoot.GetComponentInChildren<CC_UI_Manager>(true)
                : null;
            if (manager == null || charactersRoot == null)
                return;

            var index = FindExactGenderIndex(male: true);
            if (index < 0)
                index = FindExactGenderIndex(male: false);
            if (index < 0)
                index = Mathf.Clamp(DefaultCharacterIndex, 0, charactersRoot.transform.childCount - 1);

            manager.SetActiveCharacter(index);

            foreach (var cc in charactersRoot.GetComponentsInChildren<CharacterCustomization>(true))
            {
                var isSelected = cc.gameObject.activeInHierarchy;
                if (cc.UI != null)
                    cc.UI.SetActive(isSelected);
            }
        }

        private int FindExactGenderIndex(bool male)
        {
            if (charactersRoot == null)
                return -1;

            for (var i = 0; i < charactersRoot.transform.childCount; i++)
            {
                var name = charactersRoot.transform.GetChild(i).name;
                if (male && IsMaleCharacterName(name))
                    return i;
                if (!male && IsFemaleCharacterName(name))
                    return i;
            }

            return -1;
        }

        private static bool IsFemaleCharacterName(string name) =>
            name.IndexOf("Female", StringComparison.OrdinalIgnoreCase) >= 0;

        private static bool IsMaleCharacterName(string name)
        {
            // "Female" contains substring "Male" — require Male without Female.
            return name.IndexOf("Male", StringComparison.OrdinalIgnoreCase) >= 0
                   && !IsFemaleCharacterName(name);
        }

        private void EnsureBuilt()
        {
            if (_built)
                return;

            if (charactersRoot == null || uiRoot == null)
                TryLoadPrefabs();

            if (charactersRoot == null || uiRoot == null)
            {
                Debug.LogError("[CharacterCreator] Prefab references missing. Bake them into MainMenu.");
                return;
            }

            EnsureEnvironment();

            if (!_wired)
                WireCustomization();

            EnsureOverlay();
            EnsureCreatorCamera();

            charactersRoot.SetActive(false);
            uiRoot.SetActive(false);
            if (environmentRoot != null)
                environmentRoot.SetActive(false);
            if (overlayCanvas != null)
                overlayCanvas.gameObject.SetActive(false);

            _built = true;
        }

        private void TryLoadPrefabs()
        {
#if UNITY_EDITOR
            if (charactersRoot == null)
            {
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(CharactersPrefabPath);
                if (prefab != null)
                {
                    charactersRoot = Instantiate(prefab, transform);
                    charactersRoot.name = "CustomizableCharacters";
                    charactersRoot.SetActive(false);
                }
            }

            if (uiRoot == null)
            {
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(UiPrefabPath);
                if (prefab != null)
                {
                    uiRoot = Instantiate(prefab, transform);
                    uiRoot.name = "CharacterCreatorUI";
                    uiRoot.SetActive(false);
                }
            }
#endif
        }

        private void EnsureEnvironment()
        {
            if (environmentRoot != null)
                return;

#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(HalfSpherePrefabPath);
            if (prefab != null)
            {
                environmentRoot = Instantiate(prefab, transform);
                environmentRoot.name = "CreatorEnvironment";
                environmentRoot.transform.localPosition = Vector3.zero;
                environmentRoot.SetActive(false);
            }
#endif
            if (environmentRoot != null)
                return;

            environmentRoot = new GameObject("CreatorEnvironment");
            environmentRoot.transform.SetParent(transform, false);
            var light = new GameObject("CreatorLight", typeof(Light));
            light.transform.SetParent(environmentRoot.transform, false);
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var l = light.GetComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.1f;
        }

        private void WireCustomization()
        {
            var manager = uiRoot.GetComponentInChildren<CC_UI_Manager>(true);
            if (manager != null)
                manager.CharacterParent = charactersRoot;

            var uiDummy = FindChild(uiRoot.transform, "UI_Dummy");
            var uiFemale = FindChild(uiRoot.transform, "UI_Female");
            var uiMale = FindChild(uiRoot.transform, "UI_Male");

            var wasActive = charactersRoot.activeSelf;
            charactersRoot.SetActive(true);
            uiRoot.SetActive(true);

            foreach (var cc in charactersRoot.GetComponentsInChildren<CharacterCustomization>(true))
            {
                cc.Autoload = false;

                if (cc.name.IndexOf("Dummy", StringComparison.OrdinalIgnoreCase) >= 0 && uiDummy != null)
                    cc.UI = uiDummy.gameObject;
                else if (IsFemaleCharacterName(cc.name) && uiFemale != null)
                    cc.UI = uiFemale.gameObject;
                else if (IsMaleCharacterName(cc.name) && uiMale != null)
                    cc.UI = uiMale.gameObject;

                if (string.IsNullOrEmpty(cc.CharacterName))
                    cc.CharacterName = cc.name;

                // Ensure UI is active while initializing so pickers bind correctly.
                if (cc.UI != null)
                    cc.UI.SetActive(true);

                cc.Initialize();
            }

            charactersRoot.SetActive(wasActive);
            uiRoot.SetActive(false);
            _wired = true;
        }

        private void BoostUiCanvasSorting()
        {
            if (uiRoot == null)
                return;

            // Keep CC panels below the Confirm/Back overlay.
            foreach (var canvas in uiRoot.GetComponentsInChildren<Canvas>(true))
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    canvas.sortingOrder = 20;
            }
        }

        private void EnsureOverlay()
        {
            if (overlayCanvas == null)
            {
                var canvasGo = new GameObject("CreatorOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasGo.transform.SetParent(transform, false);
                overlayCanvas = canvasGo.GetComponent<Canvas>();
                overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
            }

            // Always stay above CharacterCustomizer panels so Confirm is clickable.
            overlayCanvas.sortingOrder = 500;
            overlayCanvas.gameObject.SetActive(true);
            overlayCanvas.transform.SetAsLastSibling();

            var title = overlayCanvas.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
            if (title == null)
            {
                title = CreateTmp("Title", overlayCanvas.transform, "CREATE CHARACTER", 42f, AccentRed);
                var titleRt = title.rectTransform;
                titleRt.anchorMin = new Vector2(0.5f, 1f);
                titleRt.anchorMax = new Vector2(0.5f, 1f);
                titleRt.pivot = new Vector2(0.5f, 1f);
                titleRt.anchoredPosition = new Vector2(0f, -28f);
                titleRt.sizeDelta = new Vector2(800f, 56f);
                title.alignment = TextAlignmentOptions.Center;
            }

            EnsureActionButton(
                "ConfirmButton",
                "CONFIRM",
                AccentRed,
                new Vector2(0.5f, 0f),
                new Vector2(130f, 36f),
                new Vector2(260f, 64f),
                OnConfirmClicked);

            EnsureActionButton(
                "BackButton",
                "BACK",
                TextDim,
                new Vector2(0.5f, 0f),
                new Vector2(-130f, 36f),
                new Vector2(200f, 64f),
                OnBackClicked);
        }

        private void EnsureActionButton(
            string name,
            string label,
            Color labelColor,
            Vector2 anchor,
            Vector2 anchoredPos,
            Vector2 size,
            UnityEngine.Events.UnityAction onClick)
        {
            var existing = overlayCanvas.transform.Find(name);
            Button button;
            if (existing == null)
            {
                button = CreateButton(name, overlayCanvas.transform, label, labelColor);
            }
            else
            {
                button = existing.GetComponent<Button>();
                if (button == null)
                    button = existing.gameObject.AddComponent<Button>();
            }

            var rt = button.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);
            button.interactable = true;
            button.transform.SetAsLastSibling();
        }

        private void EnsureCreatorCamera()
        {
            if (creatorCamera != null)
            {
                creatorCamera.clearFlags = CameraClearFlags.SolidColor;
                creatorCamera.backgroundColor = new Color(0.08f, 0.08f, 0.1f, 1f);
                return;
            }

            var camGo = new GameObject("CreatorCamera", typeof(Camera));
            camGo.transform.SetParent(transform, false);
            camGo.transform.position = new Vector3(0.27f, 1.44f, -3.04f);
            camGo.transform.rotation = Quaternion.Euler(10f, 355f, 0f);
            creatorCamera = camGo.GetComponent<Camera>();
            creatorCamera.fieldOfView = 38f;
            creatorCamera.clearFlags = CameraClearFlags.SolidColor;
            creatorCamera.backgroundColor = new Color(0.08f, 0.08f, 0.1f, 1f);
            creatorCamera.enabled = false;
            creatorCamera.tag = "Untagged";
        }

        private void CacheAndHideMenu()
        {
            _menuRoot ??= GameObject.Find("MainMenuRoot");
            if (_menuRoot != null)
                _menuRoot.SetActive(false);

            var canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                var bg = canvas.transform.Find("Background");
                if (bg != null)
                {
                    _menuBackground = bg.gameObject;
                    _menuBackgroundWasActive = _menuBackground.activeSelf;
                    _menuBackground.SetActive(false);
                }
            }

            var slots = FindFirstObjectByType<Modules.SaveSystem.UI.SaveSlotsPanelController>(FindObjectsInactive.Include);
            slots?.Hide();

            _menuCamera ??= Camera.main;
            if (_menuCamera == null || _menuCamera == creatorCamera)
            {
                foreach (var cam in FindObjectsByType<Camera>(FindObjectsInactive.Exclude))
                {
                    if (cam != creatorCamera)
                    {
                        _menuCamera = cam;
                        break;
                    }
                }
            }

            if (_menuCamera == null || _menuCamera == creatorCamera)
                return;

            _menuCameraWasEnabled = _menuCamera.enabled;
            _menuCamPos = _menuCamera.transform.position;
            _menuCamRot = _menuCamera.transform.rotation;
            _menuCamFov = _menuCamera.fieldOfView;
            _menuCamera.enabled = false;
            if (_menuCamera.CompareTag("MainCamera"))
                _menuCamera.tag = "Untagged";
        }

        private void ApplyCreatorCamera()
        {
            if (creatorCamera == null)
                return;

            creatorCamera.enabled = true;
            creatorCamera.tag = "MainCamera";
        }

        private void RestoreMenu()
        {
            if (creatorCamera != null)
            {
                creatorCamera.tag = "Untagged";
                creatorCamera.enabled = false;
            }

            if (_menuCamera != null && _menuCamera != creatorCamera)
            {
                _menuCamera.transform.SetPositionAndRotation(_menuCamPos, _menuCamRot);
                _menuCamera.fieldOfView = _menuCamFov;
                _menuCamera.enabled = _menuCameraWasEnabled;
                if (_menuCameraWasEnabled)
                    _menuCamera.tag = "MainCamera";
            }

            if (_menuBackground != null)
                _menuBackground.SetActive(_menuBackgroundWasActive);

            if (_menuRoot != null)
                _menuRoot.SetActive(true);
        }

        private void OnConfirmClicked()
        {
            Debug.Log($"[CharacterCreator] CONFIRM clicked. pendingSlot={_pendingNewGameSlot}");

            CC_CharacterData data = null;
            try
            {
                data = CaptureActiveCharacter();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            if (data == null)
            {
                Debug.LogWarning("[CharacterCreator] No active character to confirm.");
                return;
            }

            if (_pendingNewGameSlot < 0)
            {
                Debug.LogError("[CharacterCreator] No pending new-game slot. Open via ShowForNewGame(slot).");
                return;
            }

            if (Confirmed == null)
            {
                Debug.LogError(
                    "[CharacterCreator] Confirmed has no listeners. MenuEntryPoint is not wired — start from Bootstrap.");
                return;
            }

            var slot = _pendingNewGameSlot;
            Confirmed.Invoke(slot, CloneData(data));
        }

        private void OnBackClicked()
        {
            Hide();
            Cancelled?.Invoke();
        }

        private CC_CharacterData CaptureActiveCharacter()
        {
            if (charactersRoot == null)
                return null;

            CharacterCustomization active = null;
            foreach (var cc in charactersRoot.GetComponentsInChildren<CharacterCustomization>(true))
            {
                if (!cc.gameObject.activeInHierarchy)
                    continue;

                active = cc;
                break;
            }

            if (active == null)
                return null;

            active.StoredCharacterData ??= new CC_CharacterData();
            active.StoredCharacterData.CharacterName = string.IsNullOrEmpty(active.CharacterName)
                ? active.name
                : active.CharacterName;
            active.StoredCharacterData.CharacterPrefab = ResolveResourcesPrefabName(active.gameObject);
            active.CharacterName = active.StoredCharacterData.CharacterName;

            try
            {
                active.SaveToJSON();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[CharacterCreator] SaveToJSON failed (continuing): {exception.Message}");
            }

            var clone = CloneData(active.StoredCharacterData);
            Debug.Log(
                $"[CharacterCreator] Captured appearance prefab='{clone.CharacterPrefab}' name='{clone.CharacterName}'.");
            return clone;
        }

        private static string ResolveResourcesPrefabName(GameObject character)
        {
            var name = character.name;
            if (name.IndexOf("Female", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Female_Combined_HP";
            // "Female" already handled — "Male" substring must not match Female.
            if (name.IndexOf("Male", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Male_Combined_HP";
            if (name.IndexOf("Dummy", StringComparison.OrdinalIgnoreCase) >= 0)
                return "CC_Dummy";
            return name;
        }

        private static CC_CharacterData CloneData(CC_CharacterData source) =>
            JsonUtility.FromJson<CC_CharacterData>(JsonUtility.ToJson(source));

        private static Transform FindChild(Transform root, string name)
        {
            if (root.name == name)
                return root;

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                    return child;
            }

            return null;
        }

        private static Button CreateButton(string name, Transform parent, string label, Color labelColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.08f, 0.08f, 0.09f, 0.92f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            var tmp = CreateTmp("Label", go.transform, label, 24f, labelColor);
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
