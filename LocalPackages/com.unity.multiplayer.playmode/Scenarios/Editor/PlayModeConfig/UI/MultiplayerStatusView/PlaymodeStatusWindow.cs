using Unity.Multiplayer.PlayMode.Configurations.Editor;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation;
using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor.Views
{
    class PlaymodeStatusWindow : EditorWindow
    {
        [MenuItem("Window/Multiplayer/Play Mode Status")]
        internal static void OpenWindow()
        {
            GetWindow<PlaymodeStatusWindow>();
        }

        private PlaymodeStatusElement m_ScenarioElement;

        // Add this as a workaround to address the issue where selecting an active scenario config from the playmode popup content doesn’t refresh the playmode status window
        [InitializeOnLoadMethod]
        private static void Init()
        {
            PlayModeManager.instance.ConfigAssetChanged += () =>
            {
                var windows = Resources.FindObjectsOfTypeAll<PlaymodeStatusWindow>();
                if (windows.Length > 0)
                {
                    foreach (var window in windows)
                    {
                        window.Refresh(null);
                    }
                }
            };
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Play Mode Status");

            Refresh(null);
            Scenario.ScenarioStarted += Refresh;
        }

        private void OnFocus()
        {
            Refresh(null);
        }

        private void Refresh(Scenario scenario)
        {
            m_ScenarioElement = new PlaymodeStatusElement();
            rootVisualElement.Clear();
            rootVisualElement.Add(m_ScenarioElement);
        }
    }
}
