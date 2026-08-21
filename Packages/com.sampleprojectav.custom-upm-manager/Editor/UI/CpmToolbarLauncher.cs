#if UNITY_EDITOR && !UNITY_6000_3_OR_NEWER
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SampleProjectAV.CustomUpmManager.Editor
{
    [InitializeOnLoad]
    internal static class CpmToolbarLauncher
    {
        private const float ButtonWidth = 26f;
        private const float ButtonHeight = 20f;

        private static IMGUIContainer toolbarButton;

        static CpmToolbarLauncher()
        {
            EditorApplication.delayCall += AddToolbarButton;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void AddToolbarButton()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            var toolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
            if (toolbarType == null)
                return;

            var toolbars = Resources.FindObjectsOfTypeAll(toolbarType);
            if (toolbars.Length == 0)
                return;

            var rootField = toolbarType.GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
            var root = rootField?.GetValue(toolbars[0]) as VisualElement;
            var leftToolbarZone = root?.Q("ToolbarZoneLeftAlign");
            if (leftToolbarZone == null)
                return;

            toolbarButton?.RemoveFromHierarchy();
            toolbarButton = new IMGUIContainer(DrawButton);
            toolbarButton.style.width = ButtonWidth;
            toolbarButton.style.marginLeft = 4f;
            toolbarButton.style.marginRight = 2f;
            leftToolbarZone.Add(toolbarButton);
        }

        private static void DrawButton()
        {
            var rect = GUILayoutUtility.GetRect(ButtonWidth, ButtonHeight, EditorStyles.toolbarButton);
            if (GUI.Button(rect, new GUIContent(string.Empty, "Open CPM Installer"), EditorStyles.toolbarButton))
                CustomUpmManagerWindow.Open();

            DrawPackageGlyph(rect);
        }

        private static void DrawPackageGlyph(Rect buttonRect)
        {
            var color = EditorGUIUtility.isProSkin
                ? new Color(0.82f, 0.82f, 0.82f)
                : new Color(0.25f, 0.25f, 0.25f);
            var glyphRect = new Rect(buttonRect.center.x - 6f, buttonRect.center.y - 6f, 12f, 12f);

            EditorGUI.DrawRect(new Rect(glyphRect.x, glyphRect.y, glyphRect.width, 1f), color);
            EditorGUI.DrawRect(new Rect(glyphRect.x, glyphRect.yMax - 1f, glyphRect.width, 1f), color);
            EditorGUI.DrawRect(new Rect(glyphRect.x, glyphRect.y, 1f, glyphRect.height), color);
            EditorGUI.DrawRect(new Rect(glyphRect.xMax - 1f, glyphRect.y, 1f, glyphRect.height), color);
            EditorGUI.DrawRect(new Rect(glyphRect.center.x - 0.5f, glyphRect.y + 2f, 1f, glyphRect.height - 4f), color);
            EditorGUI.DrawRect(new Rect(glyphRect.x + 2f, glyphRect.center.y - 0.5f, glyphRect.width - 4f, 1f), color);
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode || state == PlayModeStateChange.ExitingPlayMode)
                EditorApplication.delayCall += AddToolbarButton;
        }
    }
}
#endif
