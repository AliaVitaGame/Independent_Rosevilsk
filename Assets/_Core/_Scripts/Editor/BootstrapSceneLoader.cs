#if UNITY_EDITOR
using System.IO;
using Game.Bootstrap.SceneManagement;
using Game.Shared.Bootstrap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Extensions
{
    [InitializeOnLoad]
    public static class BootstrapSceneLoader
    {
        private const string MenuItemPath = "Tools/SampleAV/Play From Bootstrap";

        static BootstrapSceneLoader()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChangedInEditMode;
            EditorApplication.delayCall += ConfigurePlayModeStartScene;
        }

        [MenuItem(MenuItemPath)]
        private static void ToggleAutoBootstrap()
        {
            var enabled = !BootstrapPlayModeSettings.IsPlayFromBootstrapEnabled();
            BootstrapPlayModeSettings.SetEnabled(enabled);
            Menu.SetChecked(MenuItemPath, enabled);
            ConfigurePlayModeStartScene();
        }

        [MenuItem(MenuItemPath, true)]
        private static bool ValidateToggle()
        {
            Menu.SetChecked(MenuItemPath, BootstrapPlayModeSettings.IsPlayFromBootstrapEnabled());
            return true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                EditorSceneManager.playModeStartScene = null;
                return;
            }

            if (state == PlayModeStateChange.ExitingEditMode)
                ConfigurePlayModeStartScene();
        }

        private static void OnActiveSceneChangedInEditMode(Scene previousScene, Scene newScene)
        {
            ConfigurePlayModeStartScene();
        }

        private static void ConfigurePlayModeStartScene()
        {
            var activeSceneName = SceneManager.GetActiveScene().name;
            var sceneCatalog = new SceneCatalog();
            var shouldBootstrap = activeSceneName == sceneCatalog.GetSceneName(SceneId.Menu)
                || activeSceneName == sceneCatalog.GetSceneName(SceneId.Game);

            if (BootstrapPlayModeSettings.IsPlayFromBootstrapEnabled() && shouldBootstrap)
            {
                var bootstrapScene = GetBuildSceneAsset(SceneId.Bootstrap);
                if (bootstrapScene != null)
                    EditorSceneManager.playModeStartScene = bootstrapScene;
                else
                    Debug.LogError($"Build Settings scene not found for {SceneId.Bootstrap}");
            }
            else
            {
                EditorSceneManager.playModeStartScene = null;
            }
        }

        private static SceneAsset GetBuildSceneAsset(SceneId sceneId)
        {
            var sceneName = new SceneCatalog().GetSceneName(sceneId);

            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (Path.GetFileNameWithoutExtension(scene.path) == sceneName)
                    return AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
            }

            return null;
        }
    }
}
#endif
