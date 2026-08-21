#if UNITY_USE_MULTIPLAYER_ROLES
using Unity.Multiplayer.Editor;
#endif
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.Api;
using Unity.Multiplayer.PlayMode.Common.Editor;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Multiplayer.PlayMode.Editor.Bridge;
using Unity.Multiplayer.PlayMode.Configurations.Editor;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor.Views
{
    /// <summary>
    /// The content of the status window that is shown, when a user clicks
    /// <see cref="MultiplayerPlayModeStatusButton"/>
    /// </summary>
    class PlaymodeStatusPopupContent : PopupWindowContent
    {
        const string k_Stylesheet = "Scenarios/Editor/PlayModeConfig/UI/MultiplayerStatusView/PlaymodeStatusPopupContent.uss";
        const string k_StylesheetDark = "Scenarios/Editor/PlayModeConfig/UI/MultiplayerStatusView/PlaymodeStatusPopupContentDark.uss";
        const string k_StylesheetLight = "Scenarios/Editor/PlayModeConfig/UI/MultiplayerStatusView/PlaymodeStatusPopupContentLight.uss";

        const string k_HeadlineName = "headline";
        private const string k_StatusButton = "status-button";
        const string k_InstanceListContainer = "status-list";

        const string k_Title = "Instances Status";

        public static readonly Vector2 windowSize = new Vector2(300, 175);
        static Dictionary<InstanceView, List<Node>> m_ViewToNode = new();

        public override Vector2 GetWindowSize()
        {
            return windowSize;
        }

        void UpdateInstanceStatus()
        {
            foreach (var (view, nodes) in m_ViewToNode)
            {
                view.SetStatus(Instance.ComputeInstanceState(nodes.Select(node => node.State).ToList()));
                view.RefreshFreeRunUI();
            }
        }

        public override VisualElement CreateGUI()
        {
            var container = new ScrollView() { name = k_InstanceListContainer };
            container.style.minHeight = container.style.maxHeight = windowSize.y;
            container.style.minWidth = container.style.maxWidth = windowSize.x;

            var headline = new Label(k_Title) { name = k_HeadlineName };

            var statusButton = new Button() { name = k_StatusButton, text = "Open Status Window" };

            var headlineContainer = new VisualElement();

            headlineContainer.AddToClassList("headline-container");

            statusButton.tooltip = "Open Play Mode Status Window";

            statusButton.RegisterCallback<ClickEvent>(evt => PlaymodeStatusWindow.OpenWindow());

            headlineContainer.Add(headline);
            headlineContainer.Add(statusButton);
            container.Add(headlineContainer);
            container.schedule.Execute(() => UpdateInstanceStatus()).Every(1000);

            UIUtils.ApplyStyleSheetAsync(k_Stylesheet, container).Forget();
            var stylesheet = EditorGUIUtility.isProSkin ? k_StylesheetDark : k_StylesheetLight;
            UIUtils.ApplyStyleSheetAsync(stylesheet, container).Forget();

            m_ViewToNode.Clear();
            var currentConfig = PlayModeManager.instance.ActivePlayModeConfig as ScenarioConfig;

            if (currentConfig == null)
                return new VisualElement() { name = "no content" };

            var instances = currentConfig.GetAllInstances();
            foreach (var instance in instances)
            {
                var instanceView = new InstanceView(instance);
                m_ViewToNode.Add(instanceView, GetNodesFromInstanceDescription(instance));
                container.Add(instanceView);
            }

            return container;
        }

        List<Node> GetNodesFromInstanceDescription(InstanceDescription instanceDescription)
        {
            var nodes = new List<Node>();

            var currentConfig = PlayModeManager.instance.ActivePlayModeConfig as ScenarioConfig;
            if (currentConfig == null || currentConfig.Scenario == null)
                return nodes;

            var instance = currentConfig.Scenario.GetInstanceByName(instanceDescription.Name);

            if (instance?.m_ExecutionGraph == null)
            {
                return nodes;
            }

            nodes = instance.m_ExecutionGraph.GetAllNodes();
            return nodes;
        }

        class InstanceView : VisualElement
        {
            internal readonly InstanceDescription m_InstanceDescription;
            readonly VisualElement m_StatusIndicator;
            private VisualElement m_DriftIndicator;
            readonly Image m_RunModeIndicator;
            const string k_InstanceViewClass = "instance-view";
            const string k_InstanceIconName = "instance-icon";
            const string k_InstanceNameName = "instance-name";
            const string k_InstanceContainerName = "instance-name-container";
            const string k_StatusContainerName = "status-container";
            const string k_StatusIndicatorName = "status-indicator";
            const string k_RunModeIndicatorName = "runmode-indicator";
            const string k_DriftIconName = "drift-icon";
            internal const string k_DriftToolTip = "This instance might be drifting. This is " +
                                                   "caused by running an instance for a long time while possible " +
                                                   "changes were detected in the Main Editor. Consider " +
                                                   "exiting and restarting the instance.";

            const string k_ActiveClass = "active";
            const string k_ErrorClass = "error";
            const string k_LoadingClass = "loading";
            const string k_IdleClass = "idle";

            internal InstanceView(InstanceDescription instance)
            {
                m_InstanceDescription = instance;
                AddToClassList(k_InstanceViewClass);

                var instanceIcon = new VisualElement() { name = k_InstanceIconName };
                instanceIcon.AddToClassList("icon");
                var nameLabel = new Label(instance.Name) { name = k_InstanceNameName };

                var instanceContainer = new VisualElement() { name = k_InstanceContainerName };
                instanceContainer.Add(instanceIcon);
                instanceContainer.Add(nameLabel);

                var statusContainer = new VisualElement() { name = k_StatusContainerName };
                m_StatusIndicator = new VisualElement() { name = k_StatusIndicatorName };
                m_StatusIndicator.AddToClassList("icon");
                m_DriftIndicator = new VisualElement() { name = k_DriftIconName };
                m_DriftIndicator.AddToClassList("icon");
                m_DriftIndicator.tooltip = k_DriftToolTip;
                m_RunModeIndicator = new Image() { name = k_RunModeIndicatorName };
                m_RunModeIndicator.AddToClassList("icon");
                m_RunModeIndicator.style.paddingRight = 2;

                statusContainer.Add(m_DriftIndicator);
                statusContainer.Add(m_RunModeIndicator);
                statusContainer.Add(m_StatusIndicator);

#if UNITY_USE_MULTIPLAYER_ROLES
                var roleLabel = new Label();
                statusContainer.Add(roleLabel);
#endif

                // Todo: this info should come from the configuration and not be branched here.
                if (instance is EditorInstanceDescription editorInstanceDescription)
                {
                    instanceIcon.style.backgroundImage = EditorGUIUtility.FindTexture("UnityLogo");
#if UNITY_USE_MULTIPLAYER_ROLES
                    roleLabel.text = editorInstanceDescription.RoleMask.ToString();
#endif
                    // Do not show run mode for the main editor
                    if (instance is MainEditorInstanceDescription)
                        m_RunModeIndicator.visible = false;
                }

                if (instance is LocalInstanceDescription localInstanceDescription)
                {
                    instanceIcon.style.backgroundImage = InternalUtilities.GetBuildProfileTypeIcon(localInstanceDescription.BuildProfile);
#if UNITY_USE_MULTIPLAYER_ROLES
                    roleLabel.text = "no role";
                    if (localInstanceDescription.BuildProfile != null)
                        roleLabel.text = EditorMultiplayerRolesManager.GetMultiplayerRoleForBuildProfile(localInstanceDescription.BuildProfile).ToString();
#endif
                }

                if (instance is RemoteInstanceDescription remoteInstanceDescription)
                {
                    instanceIcon.style.backgroundImage = InternalUtilities.GetBuildProfileTypeIcon(remoteInstanceDescription.BuildProfile);
#if UNITY_USE_MULTIPLAYER_ROLES
                    roleLabel.text = "no role";
                    if (remoteInstanceDescription.BuildProfile != null)
                        roleLabel.text = EditorMultiplayerRolesManager.GetMultiplayerRoleForBuildProfile(remoteInstanceDescription.BuildProfile).ToString();
#endif
                    var linkToDashboard = new VisualElement();
                    linkToDashboard.RegisterCallback<MouseDownEvent>(evt =>
                    {
                        var orgId = CloudProjectSettings.organizationKey;
                        var projectId = CloudProjectSettings.projectId;

                        Application.OpenURL($"https://cloud.unity.com/home/organizations/{orgId}/projects/{projectId}/multiplay/overview");
                    });


                    linkToDashboard.AddToClassList("dashboard-link");
                    linkToDashboard.AddToClassList("icon");
                    statusContainer.Add(linkToDashboard);
                }

                Add(instanceContainer);
                Add(statusContainer);
            }

            internal void SetStatus(ExecutionState status)
            {
                RemoveFromClassList(k_ActiveClass);
                RemoveFromClassList(k_ErrorClass);
                RemoveFromClassList(k_LoadingClass);
                RemoveFromClassList(k_IdleClass);
                switch (status)
                {
                    case ExecutionState.Active:
                        AddToClassList(k_ActiveClass);
                        m_StatusIndicator.tooltip = "active";
                        break;
                    case ExecutionState.Running:
                        AddToClassList(k_LoadingClass);
                        m_StatusIndicator.tooltip = "loading";
                        break;
                    case ExecutionState.Failed:
                        AddToClassList(k_ErrorClass);
                        m_StatusIndicator.tooltip = "error";
                        break;
                    default:
                        AddToClassList(k_IdleClass);
                        m_StatusIndicator.tooltip = "idle";
                        break;
                }
            }

            internal void RefreshFreeRunUI()
            {
                // Grab the running mode and update the visual icon if it's changed.
                var currRunMode = m_InstanceDescription.RunModeState;
                m_RunModeIndicator.SetRunModeIcon(currRunMode);

                // Grab the tool tip for the current mode and update if it's changed.
                var toolTipText = GetRunModeToolTip(currRunMode, m_InstanceDescription);
                if (!m_RunModeIndicator.tooltip.Equals(toolTipText))
                    m_RunModeIndicator.tooltip = toolTipText;

                // Refresh the coherence drift UI for free run instances
                var instance = ScenarioRunner.instance.ActiveScenario?.GetInstanceByName(m_InstanceDescription.Name);
                if (instance != null)
                    m_DriftIndicator.visible = instance.Drifted;
            }

            private string GetRunModeToolTip(RunModeState runMode, InstanceDescription instanceDescription)
            {
                switch (runMode)
                {
                    case RunModeState.ScenarioControl:
                        if (instanceDescription.InstanceTypeName == EditorInstanceDescription.k_EditorInstanceTypeName)
                            return "This instance is controlled by the main editor process.\n" +
                                   "It will be activated when entering play mode.";
                        if (instanceDescription.InstanceTypeName == LocalInstanceDescription.k_LocalInstanceTypeName)
                            return "This instance is controlled by the main editor process.\n" +
                                   "It will build and run when entering play mode.";
                        if (instanceDescription.InstanceTypeName == RemoteInstanceDescription.k_RemoteInstanceTypeName)
                            return "This instance is controlled by the main editor process.\n" +
                                   "It will build and deploy when entering play mode";

                        // Default tool tip for manually controlled instances.
                        return "This instance is controlled by the main editor process.";
                    case RunModeState.ManualControl:
                        return "This instance is controlled manually. It can be controlled separately from play mode.";
                }

                return String.Empty;
            }
        }

    }
}
