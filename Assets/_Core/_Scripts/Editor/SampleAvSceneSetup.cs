#if UNITY_EDITOR
using System;
using System.Linq;
using Shared.UI.Loading;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Extensions
{
    [InitializeOnLoad]
    public static class SampleAvSceneSetup
    {
        private const int SetupVersion = 2;
        private const string SetupVersionKey = "SampleAV_SceneSetupVersion";
        private const string BootstrapScenePath = "Assets/_Core/Scenes/Bootstrap.unity";
        private const string MenuScenePath = "Assets/_Core/Scenes/MainMenu.unity";
        private const string GameScenePath = "Assets/_Core/Scenes/Game.unity";
        private const string LoadingPrefabPath = "Assets/_Core/Prefabs/UI/Shared/LoadingPanelCanvas.prefab";
        private const string MenuCanvasName = "SampleAV Menu Canvas";

        static SampleAvSceneSetup()
        {
            EditorApplication.delayCall += RunAutomaticSetup;
        }

        [MenuItem("Tools/SampleAV/Rebuild Scene Flow")]
        public static void RebuildSceneFlow()
        {
            ConfigureBuildSettings();
            ConfigureLoadingPrefab();
            ConfigureMenuScene();
            EditorPrefs.SetInt(SetupVersionKey, SetupVersion);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("SampleAV scene flow configured: Bootstrap -> MainMenu -> Game.");
        }

        private static void RunAutomaticSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                return;

            if (EditorPrefs.GetInt(SetupVersionKey, 0) >= SetupVersion)
                return;

            try
            {
                RebuildSceneFlow();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                CreateBuildScene(BootstrapScenePath),
                CreateBuildScene(MenuScenePath),
                CreateBuildScene(GameScenePath)
            };
        }

        private static EditorBuildSettingsScene CreateBuildScene(string path)
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            if (sceneAsset == null)
                throw new InvalidOperationException($"Scene not found: {path}");

            return new EditorBuildSettingsScene(path, true);
        }

        private static void ConfigureLoadingPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(LoadingPrefabPath);

            try
            {
                var view = root.GetComponent<LoadingPanelView>();
                if (view == null)
                    throw new InvalidOperationException($"LoadingPanelView is missing on {LoadingPrefabPath}");

                DestroyChildren(root.transform);
                Stretch(root.GetComponent<RectTransform>());

                var background = CreateImage("Loading Background", root.transform, new Color(0.035f, 0.055f, 0.09f, 1f));
                Stretch(background.rectTransform);

                var barBackground = CreateImage("Progress Bar Background", background.transform, new Color(1f, 1f, 1f, 0.16f));
                SetRect(barBackground.rectTransform, new Vector2(0.5f, 0.16f), new Vector2(720f, 30f), Vector2.zero);

                var progress = CreateImage("Progress", barBackground.transform, new Color(0.2f, 0.72f, 1f, 1f));
                Stretch(progress.rectTransform, new Vector2(8f, 8f));
                progress.type = Image.Type.Filled;
                progress.fillMethod = Image.FillMethod.Horizontal;
                progress.fillOrigin = (int)Image.OriginHorizontal.Left;
                progress.fillAmount = 0f;

                var serializedView = new SerializedObject(view);
                serializedView.FindProperty("loadingImage").objectReferenceValue = progress;
                serializedView.FindProperty("loadingPanel").objectReferenceValue = background.gameObject;
                serializedView.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, LoadingPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureMenuScene()
        {
            var originalActiveScene = SceneManager.GetActiveScene();
            var menuScene = SceneManager.GetSceneByPath(MenuScenePath);
            var openedForSetup = !menuScene.isLoaded;

            if (openedForSetup)
                menuScene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Additive);

            try
            {
                var oldCanvas = menuScene.GetRootGameObjects().FirstOrDefault(root => root.name == MenuCanvasName);
                if (oldCanvas != null)
                    UnityEngine.Object.DestroyImmediate(oldCanvas);

                var canvasObject = new GameObject(MenuCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                SceneManager.MoveGameObjectToScene(canvasObject, menuScene);

                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                var scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;

                var background = CreateImage("Menu Background", canvasObject.transform, new Color(0.055f, 0.075f, 0.12f, 1f));
                Stretch(background.rectTransform);

                var buttonObject = new GameObject("StartButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                buttonObject.layer = LayerMask.NameToLayer("UI");
                buttonObject.transform.SetParent(background.transform, false);
                SetRect(buttonObject.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(300f, 92f), new Vector2(190f, 0f));

                var buttonImage = buttonObject.GetComponent<Image>();
                buttonImage.color = new Color(0.12f, 0.55f, 0.88f, 1f);

                var button = buttonObject.GetComponent<Button>();
                var colors = button.colors;
                colors.highlightedColor = new Color(0.2f, 0.68f, 1f, 1f);
                colors.pressedColor = new Color(0.08f, 0.42f, 0.72f, 1f);
                button.colors = colors;

                var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                labelObject.layer = LayerMask.NameToLayer("UI");
                labelObject.transform.SetParent(buttonObject.transform, false);
                Stretch(labelObject.GetComponent<RectTransform>());

                var label = labelObject.GetComponent<Text>();
                label.text = "START";
                label.alignment = TextAnchor.MiddleCenter;
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.fontSize = 34;
                label.fontStyle = FontStyle.Bold;
                label.color = Color.white;

                EnsureEventSystem(menuScene);
                EditorSceneManager.MarkSceneDirty(menuScene);
                EditorSceneManager.SaveScene(menuScene);
            }
            finally
            {
                if (openedForSetup)
                    EditorSceneManager.CloseScene(menuScene, true);

                if (originalActiveScene.IsValid() && originalActiveScene.isLoaded)
                    SceneManager.SetActiveScene(originalActiveScene);
            }
        }

        private static void EnsureEventSystem(Scene scene)
        {
            if (scene.GetRootGameObjects().Any(root => root.GetComponentInChildren<EventSystem>(true) != null))
                return;

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            SceneManager.MoveGameObjectToScene(eventSystem, scene);
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.layer = LayerMask.NameToLayer("UI");
            gameObject.transform.SetParent(parent, false);

            var image = gameObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static void DestroyChildren(Transform parent)
        {
            for (var index = parent.childCount - 1; index >= 0; index--)
                UnityEngine.Object.DestroyImmediate(parent.GetChild(index).gameObject);
        }

        private static void Stretch(RectTransform rectTransform, Vector2 padding = default)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = -padding;
        }

        private static void SetRect(RectTransform rectTransform, Vector2 anchor, Vector2 size, Vector2 position)
        {
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = position;
        }
    }
}
#endif
