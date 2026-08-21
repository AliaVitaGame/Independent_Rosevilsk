using System;
#if UNITY_USE_MULTIPLAYER_ROLES
using Unity.Multiplayer.Editor;
#endif
using Unity.Multiplayer.PlayMode.Editor.Bridge;
using UnityEditor.Build.Profile;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor
{
    [Serializable]
    class LocalInstanceDescription : InstanceDescription, IBuildableInstanceDescription
    {
        //[Tooltip("Select the Build profile that this instance will be based  as.")]
        [SerializeField] private BuildProfile m_BuildProfile;

        [SerializeField] private AdvancedConfig advancedConfiguration = new();
        internal const string k_LocalInstanceTypeName = "Local";

        [Serializable]
        public class AdvancedConfig
        {
            [Tooltip("Box checked : The logs will be streamed from local instance to the editor logs \nUnchecked : The logs will be captured from local instance into the logfile under {InstanceName}.txt")]
            [SerializeField] private bool m_StreamLogsToMainEditor = true;
            [SerializeField] private Color m_LogsColor = new Color(0.3643f, 0.581f, 0.8679f);
            [SerializeField] private string m_Arguments = "-screen-fullscreen 0 -screen-width 1024 -screen-height 720";
            [SerializeField, HideInInspector] private string m_DeviceID = "";
            [SerializeField, HideInInspector] private string m_DeviceName = "";

            public bool StreamLogsToMainEditor
            {
                get => m_StreamLogsToMainEditor;
                set => m_StreamLogsToMainEditor = value;
            }

            public string DeviceID
            {
                get => m_DeviceID;
                set => m_DeviceID = value;
            }

            public string DeviceName
            {
                get => m_DeviceName;
                set => m_DeviceName = value;
            }

            public string Arguments
            {
                get => m_Arguments;
                set => m_Arguments = value;
            }

            public Color LogsColor
            {
                get => m_LogsColor;
                set => m_LogsColor = value;
            }
        }

        public BuildProfile BuildProfile
        {
            get => m_BuildProfile;
            set => m_BuildProfile = value;
        }

        public AdvancedConfig AdvancedConfiguration
        {
            get => advancedConfiguration;
            set => advancedConfiguration = value;
        }

        internal override string InstanceTypeName => k_LocalInstanceTypeName;

        internal override string BuildTargetType => InternalUtilities.GetBuildTargetType(m_BuildProfile);

#if UNITY_USE_MULTIPLAYER_ROLES
        internal override string MultiplayerRole => EditorMultiplayerRolesManager.GetMultiplayerRoleStringForBuildProfile(m_BuildProfile);
#endif
    }
}
