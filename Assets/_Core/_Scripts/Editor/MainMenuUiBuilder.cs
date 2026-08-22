#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Extensions
{
    public static class MainMenuUiBuilder
    {
        private const string ScenePath = "Assets/_Core/Scenes/MainMenu.unity";
        private const string Bloodlines = "Assets/_Core/_Content/Arts/UI/Bloodlines UI/";
        private const string HalfSpherePath =
            "Assets/CharacterCustomizer/Environment/HalfSphere/HalfSphere.prefab";
        private const string RobotoBold =
            "Assets/CharacterCustomizer/UI/Fonts/Roboto/Roboto-Bold SDF.asset";
        private const string RobotoRegular =
            "Assets/CharacterCustomizer/UI/Fonts/Roboto/Roboto-Regular SDF.asset";
        private const string LiberationSans =
            "Assets/Plugins/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        private static readonly Color AccentRed = new(0.72f, 0.08f, 0.08f, 1f);
        private static readonly Color TextLight = new(0.86f, 0.86f, 0.84f, 1f);
        private static readonly Color TextDim = new(0.55f, 0.55f, 0.53f, 1f);
        private static readonly Color PanelBg = new(0.03f, 0.03f, 0.035f, 0.82f);

        [MenuItem("Tools/Main Menu/Build Project Z Rescue UI")]
        public static void BuildFromMenu()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            BuildInOpenedScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("MainMenu UI built and saved.");
        }

        public static string BuildInOpenedScene()
        {
            EnsureEventSystem();
            SetupEnvironment();
            BuildCanvasUi();
            return "ok";
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
                return;

            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
        }

        private static void SetupEnvironment()
        {
            var oldMenuUi = GameObject.Find("MainMenuUI");
            if (oldMenuUi != null)
                oldMenuUi.SetActive(false);

            var particles = GameObject.Find("Background Particle System");
            if (particles != null)
                particles.SetActive(false);

            DestroyIfExists("MenuEnvironment");
            DestroyIfExists("MenuCafeCar");

            var env = new GameObject("MenuEnvironment");
            Undo.RegisterCreatedObjectUndo(env, "Create MenuEnvironment");

            var halfSpherePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HalfSpherePath);
            if (halfSpherePrefab != null)
            {
                var half = (GameObject)PrefabUtility.InstantiatePrefab(halfSpherePrefab);
                half.name = "HalfSphere";
                half.transform.SetParent(env.transform, false);
                half.transform.localScale = Vector3.one * 1.35f;
            }

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(env.transform, false);
            ground.transform.localScale = new Vector3(10f, 1f, 10f);
            Object.DestroyImmediate(ground.GetComponent<Collider>());
            var groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            groundMat.color = new Color(0.035f, 0.035f, 0.04f, 1f);
            if (groundMat.HasProperty("_Smoothness"))
                groundMat.SetFloat("_Smoothness", 0.7f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

            var carPrefab = FindCarPrefab();
            if (carPrefab == null)
                throw new System.InvalidOperationException("Caffe Car Driveable prefab not found.");

            var car = (GameObject)PrefabUtility.InstantiatePrefab(carPrefab);
            car.name = "MenuCafeCar";
            car.transform.position = new Vector3(3.6f, 0f, 8.2f);
            car.transform.rotation = Quaternion.Euler(0f, -130f, 0f);
            car.transform.localScale = Vector3.one * 1.25f;
            DisableDrivingBehaviours(car);
            EnableCarLights(car);

            var camera = Camera.main;
            if (camera != null)
            {
                camera.transform.position = new Vector3(-1.8f, 1.35f, 3.4f);
                camera.transform.rotation = Quaternion.Euler(4f, 32f, 0f);
                camera.fieldOfView = 40f;
                camera.backgroundColor = new Color(0.015f, 0.015f, 0.02f, 1f);
                camera.farClipPlane = 220f;
            }

            var light = Object.FindFirstObjectByType<Light>();
            if (light != null && light.type == LightType.Directional)
            {
                light.transform.rotation = Quaternion.Euler(26f, -40f, 0f);
                light.color = new Color(0.5f, 0.55f, 0.68f, 1f);
                light.intensity = 0.45f;
                light.shadows = LightShadows.Soft;
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.042f;
            RenderSettings.fogColor = new Color(0.04f, 0.045f, 0.06f, 1f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.1f, 0.11f, 0.14f, 1f);

            var keyGo = new GameObject("CarKeyLight");
            keyGo.transform.SetParent(env.transform, false);
            keyGo.transform.position = new Vector3(1.6f, 2.7f, 5.2f);
            var key = keyGo.AddComponent<Light>();
            key.type = LightType.Spot;
            key.range = 20f;
            key.spotAngle = 75f;
            key.intensity = 2.8f;
            key.color = new Color(1f, 0.94f, 0.82f, 1f);
            keyGo.transform.LookAt(new Vector3(3.6f, 0.75f, 8.2f));

            var rimGo = new GameObject("CarRimLight");
            rimGo.transform.SetParent(env.transform, false);
            rimGo.transform.position = new Vector3(5.8f, 1.6f, 8.8f);
            var rim = rimGo.AddComponent<Light>();
            rim.type = LightType.Point;
            rim.range = 12f;
            rim.intensity = 1.8f;
            rim.color = new Color(0.7f, 0.1f, 0.08f, 1f);
        }

        private static GameObject FindCarPrefab()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.IndexOf("Driveable", System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                if (path.IndexOf("Caff", System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }

            return null;
        }

        private static void DisableDrivingBehaviours(GameObject car)
        {
            foreach (var behaviour in car.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null)
                    continue;

                var typeName = behaviour.GetType().Name;
                if (typeName.Contains("Controller") ||
                    typeName.Contains("Drive") ||
                    typeName.Contains("Input") ||
                    typeName.Contains("Wheel") ||
                    typeName.Contains("Audio") ||
                    typeName.Contains("Engine"))
                {
                    behaviour.enabled = false;
                }
            }

            foreach (var rb in car.GetComponentsInChildren<Rigidbody>(true))
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        private static void EnableCarLights(GameObject car)
        {
            foreach (var light in car.GetComponentsInChildren<Light>(true))
            {
                light.enabled = true;
                if (light.type is LightType.Spot or LightType.Point)
                    light.intensity = Mathf.Max(light.intensity, 2.2f);
            }
        }

        private static void BuildCanvasUi()
        {
            var canvasGo = GameObject.Find("Canvas");
            if (canvasGo == null)
            {
                canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                Undo.RegisterCreatedObjectUndo(canvasGo, "Create Canvas");
            }

            for (var i = canvasGo.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(canvasGo.transform.GetChild(i).gameObject);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var root = CreateUiObject("MainMenuRoot", canvasGo.transform);
            StretchFull(root.GetComponent<RectTransform>());

            CreateLeftScrim(root.transform);
            CreateTitle(root.transform);
            CreateMenuButtons(root.transform);
            CreateProfileCard(root.transform);
            CreateFooter(root.transform);
        }

        private static void CreateLeftScrim(Transform parent)
        {
            var go = CreateUiObject("LeftScrim", parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0.48f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var image = go.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.18f);
            image.raycastTarget = false;
        }

        private static void CreateTitle(Transform parent)
        {
            var font = LoadFont(Bloodlines + "Fonts/ManufacturingConsent SDF.asset")
                       ?? LoadFont(Bloodlines + "Fonts/MedievalSharp SDF.asset");

            var title = CreateUiObject("Title", parent);
            var rt = title.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(70f, -42f);
            rt.sizeDelta = new Vector2(760f, 280f);

            PlaceTitleLine(CreateTmp("Project", title.transform, "PROJECT", font, 56f, TextLight), 0f, 56f);
            PlaceTitleLine(CreateTmp("Z", title.transform, "Z", font, 140f, AccentRed), 52f, 140f);
            PlaceTitleLine(CreateTmp("Rescue", title.transform, "RESCUE", font, 56f, TextLight), 200f, 56f);
        }

        private static void PlaceTitleLine(TextMeshProUGUI tmp, float y, float height)
        {
            var rt = tmp.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(0f, -y);
            rt.sizeDelta = new Vector2(0f, height);
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.fontStyle = FontStyles.UpperCase;
            tmp.enableWordWrapping = false;
            tmp.characterSpacing = -1.5f;
        }

        private static void CreateMenuButtons(Transform parent)
        {
            var font = LoadFont(RobotoBold) ?? LoadFont(LiberationSans);
            var outlineRed = LoadSprite(Bloodlines + "Textures/Frame/Frame_outline_red.png");

            var list = CreateUiObject("MenuButtons", parent);
            var listRt = list.GetComponent<RectTransform>();
            listRt.anchorMin = new Vector2(0f, 0.5f);
            listRt.anchorMax = new Vector2(0f, 0.5f);
            listRt.pivot = new Vector2(0f, 0.5f);
            listRt.anchoredPosition = new Vector2(70f, -30f);
            listRt.sizeDelta = new Vector2(430f, 430f);

            var layout = list.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var labels = new[] { "CONTINUE", "NEW GAME", "LOAD GAME", "NPCS", "SETTINGS", "EXIT" };
            for (var i = 0; i < labels.Length; i++)
                CreateMenuButton(list.transform, labels[i], font, i == 0, outlineRed);
        }

        private static void CreateMenuButton(
            Transform parent,
            string label,
            TMP_FontAsset font,
            bool selected,
            Sprite outlineRed)
        {
            var go = CreateUiObject(label.Replace(" ", "") + "Button", parent);
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 56f;
            le.preferredHeight = 56f;

            var button = go.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;

            var frame = CreateUiObject("Frame", go.transform);
            StretchFull(frame.GetComponent<RectTransform>());
            var frameImage = frame.AddComponent<Image>();
            frameImage.sprite = outlineRed;
            frameImage.type = Image.Type.Sliced;
            frameImage.color = selected ? Color.white : new Color(1f, 1f, 1f, 0f);
            frameImage.raycastTarget = false;

            var hit = CreateUiObject("Hit", go.transform);
            StretchFull(hit.GetComponent<RectTransform>());
            var hitImage = hit.AddComponent<Image>();
            hitImage.color = new Color(1f, 1f, 1f, 0.001f);
            button.targetGraphic = hitImage;

            var text = CreateTmp("Label", go.transform, label, font, 32f, selected ? AccentRed : TextLight);
            var textRt = text.rectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(20f, 2f);
            textRt.offsetMax = new Vector2(-42f, -2f);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.fontStyle = FontStyles.UpperCase;
            text.enableWordWrapping = false;

            var arrow = CreateTmp("Arrow", go.transform, ">", font, 28f, AccentRed);
            var arrowRt = arrow.rectTransform;
            arrowRt.anchorMin = new Vector2(1f, 0f);
            arrowRt.anchorMax = new Vector2(1f, 1f);
            arrowRt.pivot = new Vector2(1f, 0.5f);
            arrowRt.anchoredPosition = new Vector2(-16f, 0f);
            arrowRt.sizeDelta = new Vector2(24f, 0f);
            arrow.alignment = TextAlignmentOptions.Center;
            arrow.gameObject.SetActive(selected);
        }

        private static void CreateProfileCard(Transform parent)
        {
            var outline = LoadSprite(Bloodlines + "Textures/Frame/Frame_outline.png");
            var barEmpty = LoadSprite(Bloodlines + "Textures/Progress_Bar/Rectangle/Progress_Bar_Rectangle_empty_v1.png");
            var barFull = LoadSprite(Bloodlines + "Textures/Progress_Bar/Rectangle/Progress_Bar_Rectangle_full_v1.png");
            Sprite avatarSprite = null;
            var fontBold = LoadFont(RobotoBold) ?? LoadFont(LiberationSans);
            var fontReg = LoadFont(RobotoRegular) ?? fontBold;

            var card = CreateUiObject("ProfileCard", parent);
            var rt = card.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-48f, -40f);
            rt.sizeDelta = new Vector2(390f, 110f);

            var bg = card.AddComponent<Image>();
            bg.color = PanelBg;

            var frame = CreateUiObject("Frame", card.transform);
            StretchFull(frame.GetComponent<RectTransform>());
            var frameImage = frame.AddComponent<Image>();
            frameImage.sprite = outline;
            frameImage.type = Image.Type.Sliced;
            frameImage.color = new Color(0.78f, 0.78f, 0.78f, 0.95f);
            frameImage.raycastTarget = false;

            var avatar = CreateUiObject("Avatar", card.transform);
            var avatarRt = avatar.GetComponent<RectTransform>();
            avatarRt.anchorMin = new Vector2(0f, 0.5f);
            avatarRt.anchorMax = new Vector2(0f, 0.5f);
            avatarRt.pivot = new Vector2(0f, 0.5f);
            avatarRt.anchoredPosition = new Vector2(16f, 0f);
            avatarRt.sizeDelta = new Vector2(74f, 74f);
            var avatarBg = avatar.AddComponent<Image>();
            avatarBg.color = new Color(0.08f, 0.08f, 0.09f, 1f);

            var avatarIcon = CreateUiObject("Icon", avatar.transform);
            StretchFull(avatarIcon.GetComponent<RectTransform>(), 8f);
            var avatarImage = avatarIcon.AddComponent<Image>();
            avatarImage.sprite = avatarSprite;
            avatarImage.color = new Color(0.55f, 0.55f, 0.55f, 1f);
            avatarImage.preserveAspect = true;
            avatarImage.raycastTarget = false;

            var name = CreateTmp("PlayerName", card.transform, "DRON NK", fontBold, 28f, Color.white);
            var nameRt = name.rectTransform;
            nameRt.anchorMin = new Vector2(0f, 1f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.pivot = new Vector2(0f, 1f);
            nameRt.anchoredPosition = new Vector2(104f, -14f);
            nameRt.sizeDelta = new Vector2(-120f, 32f);
            name.alignment = TextAlignmentOptions.MidlineLeft;
            name.fontStyle = FontStyles.UpperCase;

            var rank = CreateTmp("Rank", card.transform, "RANK 4", fontReg, 18f, TextDim);
            var rankRt = rank.rectTransform;
            rankRt.anchorMin = new Vector2(0f, 1f);
            rankRt.anchorMax = new Vector2(1f, 1f);
            rankRt.pivot = new Vector2(0f, 1f);
            rankRt.anchoredPosition = new Vector2(104f, -44f);
            rankRt.sizeDelta = new Vector2(-120f, 22f);
            rank.alignment = TextAlignmentOptions.MidlineLeft;
            rank.fontStyle = FontStyles.UpperCase;

            var xpRoot = CreateUiObject("XpBar", card.transform);
            var xpRt = xpRoot.GetComponent<RectTransform>();
            xpRt.anchorMin = new Vector2(0f, 0f);
            xpRt.anchorMax = new Vector2(1f, 0f);
            xpRt.pivot = new Vector2(0f, 0f);
            xpRt.anchoredPosition = new Vector2(104f, 18f);
            xpRt.sizeDelta = new Vector2(-220f, 14f);

            var xpBg = xpRoot.AddComponent<Image>();
            if (barEmpty != null)
            {
                xpBg.sprite = barEmpty;
                xpBg.type = Image.Type.Sliced;
                xpBg.color = Color.white;
            }
            else
            {
                xpBg.color = new Color(0.16f, 0.16f, 0.16f, 1f);
            }

            var fill = CreateUiObject("Fill", xpRoot.transform);
            var fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(0.35f, 1f);
            fillRt.offsetMin = new Vector2(2f, 2f);
            fillRt.offsetMax = new Vector2(-2f, -2f);
            var fillImage = fill.AddComponent<Image>();
            if (barFull != null)
            {
                fillImage.sprite = barFull;
                fillImage.type = Image.Type.Sliced;
                fillImage.color = Color.white;
            }
            else
            {
                fillImage.color = AccentRed;
            }

            var xpText = CreateTmp("XpValue", card.transform, "350 / 1000", fontReg, 16f, TextDim);
            var xpTextRt = xpText.rectTransform;
            xpTextRt.anchorMin = new Vector2(1f, 0f);
            xpTextRt.anchorMax = new Vector2(1f, 0f);
            xpTextRt.pivot = new Vector2(1f, 0f);
            xpTextRt.anchoredPosition = new Vector2(-14f, 16f);
            xpTextRt.sizeDelta = new Vector2(100f, 20f);
            xpText.alignment = TextAlignmentOptions.MidlineRight;
        }

        private static void CreateFooter(Transform parent)
        {
            var font = LoadFont(RobotoRegular) ?? LoadFont(LiberationSans);
            var settingsIcon = LoadSprite(Bloodlines + "Textures/Icon/Icons_settins_frame.png")
                               ?? LoadSprite(Bloodlines + "Textures/Icon/Icons-5.png");

            var discord = CreateUiObject("Discord", parent);
            var discordRt = discord.GetComponent<RectTransform>();
            discordRt.anchorMin = new Vector2(0f, 0f);
            discordRt.anchorMax = new Vector2(0f, 0f);
            discordRt.pivot = new Vector2(0f, 0f);
            discordRt.anchoredPosition = new Vector2(56f, 34f);
            discordRt.sizeDelta = new Vector2(220f, 40f);

            var icon = CreateUiObject("Icon", discord.transform);
            var iconRt = icon.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.anchoredPosition = Vector2.zero;
            iconRt.sizeDelta = new Vector2(28f, 28f);
            var iconImage = icon.AddComponent<Image>();
            iconImage.sprite = settingsIcon;
            iconImage.color = TextDim;
            iconImage.preserveAspect = true;

            var discordText = CreateTmp("Label", discord.transform, "DISCORD", font, 18f, TextDim);
            var discordTextRt = discordText.rectTransform;
            discordTextRt.anchorMin = Vector2.zero;
            discordTextRt.anchorMax = Vector2.one;
            discordTextRt.offsetMin = new Vector2(36f, 0f);
            discordTextRt.offsetMax = Vector2.zero;
            discordText.alignment = TextAlignmentOptions.MidlineLeft;
            discordText.fontStyle = FontStyles.UpperCase;

            var version = CreateTmp("Version", parent, "v0.1.0", font, 18f, TextDim);
            var versionRt = version.rectTransform;
            versionRt.anchorMin = new Vector2(1f, 0f);
            versionRt.anchorMax = new Vector2(1f, 0f);
            versionRt.pivot = new Vector2(1f, 0f);
            versionRt.anchoredPosition = new Vector2(-48f, 34f);
            versionRt.sizeDelta = new Vector2(120f, 30f);
            version.alignment = TextAlignmentOptions.MidlineRight;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            return go;
        }

        private static TextMeshProUGUI CreateTmp(
            string name,
            Transform parent,
            string content,
            TMP_FontAsset font,
            float size,
            Color color)
        {
            var go = CreateUiObject(name, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.font = font;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.raycastTarget = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            return tmp;
        }

        private static void StretchFull(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        private static Sprite LoadSprite(string path)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            if (sprites.Length > 0)
                return sprites[0];

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static TMP_FontAsset LoadFont(string path)
        {
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        }

        private static void DestroyIfExists(string name)
        {
            var go = GameObject.Find(name);
            if (go != null)
                Object.DestroyImmediate(go);
        }
    }
}
#endif
