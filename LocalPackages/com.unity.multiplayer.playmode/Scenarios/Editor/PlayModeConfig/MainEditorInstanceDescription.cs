using System;
using Unity.Multiplayer.PlayMode.Editor.Bridge;
using UnityEditor;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor
{
    [Serializable]
    class MainEditorInstanceDescription : EditorInstanceDescription
    {
        internal const string k_MainEditorInstanceTypeName = "MainEditor";

        internal override string InstanceTypeName => k_MainEditorInstanceTypeName;

#if UNITY_USE_MULTIPLAYER_ROLES
        internal override string MultiplayerRole => m_Role.ToString();
#endif
        internal override string BuildTargetType => InternalUtilities.GetBuildTargetType(EditorUserBuildSettings.activeBuildTarget);
    }
}
