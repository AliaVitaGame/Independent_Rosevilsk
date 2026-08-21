using System;
using Unity.Multiplayer.PlayMode.Editor.Bridge;
using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor
{
    [Serializable]
    class VirtualEditorInstanceDescription : EditorInstanceDescription
    {
        internal const string k_VirtualEditorInstanceDescription = "VirtualEditor";
        [Serializable]
        public class AdvancedConfig
        {
            [InspectorName("Stream Logs To Main Editor")] public bool StreamLogsToMainEditor;
            public Color LogsColor = new(0.3643f, 0.581f, 0.8679f);
        }

        [SerializeField] private AdvancedConfig m_AdvancedConfiguration;

        public AdvancedConfig AdvancedConfiguration => m_AdvancedConfiguration;

        internal override string InstanceTypeName => k_VirtualEditorInstanceDescription;

#if UNITY_USE_MULTIPLAYER_ROLES
        internal override string MultiplayerRole => m_Role.ToString();
#endif

        internal override string BuildTargetType => InternalUtilities.GetBuildTargetType(EditorUserBuildSettings.activeBuildTarget);
    }
}
