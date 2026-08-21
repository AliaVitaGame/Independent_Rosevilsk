// TODO: MTT-8667 This should be moved to a proper assembly and remove these scripting defines.
#if MULTIPLAY_API_AVAILABLE && UNITY_EDITOR
using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.Api;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.Multiplay;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.Nodes.Editor;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.Nodes.Multiplay;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.Nodes.Multiplay.Exceptions;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.Utils;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor.Instances
{
    class RemoteInstanceController : PlayModeController
    {
        private readonly RemoteInstanceDescription m_Settings;
        private IMultiplayValidationHelper m_MultiplayValidationHelper;

        internal RemoteInstanceController(RemoteInstanceDescription remoteInstanceDescription,
            IMultiplayValidationHelper multiplayValidationHelper = null)
        {
            m_MultiplayValidationHelper = multiplayValidationHelper ?? new MultiplayValidationHelper();
            m_Settings = remoteInstanceDescription;
        }

        protected virtual IMultiplayValidationHelper MultiplayValidationHelper => m_MultiplayValidationHelper ??= new MultiplayValidationHelper();

        protected internal override void SetupExecutionGraph(ExecutionGraph executionGraph)
        {
#if UNITY_USE_MULTIPLAYER_ROLES
            var role = ScenarioFactory.GetRoleForInstance(m_Settings);

            // We assume the remote instance is a server.
            Assert.AreEqual(role, MultiplayerRoleFlags.Server);
#endif
            var buildPath = ScenarioFactory.GenerateBuildPath(m_Settings.BuildProfile);

            var buildNode = new EditorBuildNode($"{m_Settings.Name} {ScenarioFactory.RemoteNodeConstants.k_BuildNodePostFix}");
            executionGraph.AddNode(buildNode, ExecutionStage.Prepare);
            executionGraph.ConnectConstant(buildNode.BuildPath, buildPath);
            executionGraph.ConnectConstant(buildNode.Profile, m_Settings.BuildProfile);


            var advancedConfiguration = m_Settings.AdvancedConfiguration;
            var multiplayName = RemoteInstanceDescription.ComputeMultiplayName(advancedConfiguration.Identifier);
            var buildName = multiplayName;
            var buildConfigurationName = multiplayName;
            var fleetName = multiplayName;

            var deployBuildNode = new DeployBuildNode($"{m_Settings.Name} {ScenarioFactory.RemoteNodeConstants.k_DeployBuildNodePostfix}");
            executionGraph.AddNode(deployBuildNode, ExecutionStage.Deploy);
            executionGraph.ConnectConstant(deployBuildNode.BuildName, buildName);
            executionGraph.Connect(buildNode.OutputPath, deployBuildNode.BuildPath);
            executionGraph.Connect(buildNode.ExecutablePath, deployBuildNode.ExecutablePath);
            executionGraph.Connect(buildNode.BuildHash, deployBuildNode.BuildHash);

            var deployBuildConfigNode = new DeployBuildConfigurationNode($"{m_Settings.Name} {ScenarioFactory.RemoteNodeConstants.k_DeployConfigBuildNodePostfix}");
            executionGraph.AddNode(deployBuildConfigNode, ExecutionStage.Deploy);
            executionGraph.ConnectConstant(deployBuildConfigNode.BuildConfigurationName, buildConfigurationName);
            executionGraph.ConnectConstant(deployBuildConfigNode.BuildName, buildName);
            executionGraph.ConnectConstant(deployBuildConfigNode.Settings, m_Settings.GetBuildConfigurationSettings());
            executionGraph.Connect(deployBuildNode.BuildId, deployBuildConfigNode.BuildId);
            executionGraph.Connect(buildNode.RelativeExecutablePath, deployBuildConfigNode.BinaryPath);

            var deployFleetNode = new DeployFleetNode($"{m_Settings.Name} {ScenarioFactory.RemoteNodeConstants.k_DeployFleetNodePostfix}");
            executionGraph.AddNode(deployFleetNode, ExecutionStage.Deploy);
            executionGraph.ConnectConstant(deployFleetNode.FleetName, fleetName);
            executionGraph.ConnectConstant(deployFleetNode.Region, advancedConfiguration.FleetRegion);
            executionGraph.ConnectConstant(deployFleetNode.BuildConfigurationName, buildConfigurationName);
            executionGraph.Connect(deployBuildConfigNode.BuildConfigurationId, deployFleetNode.BuildConfigurationId);


            var allocateNode = new AllocateNode($"{m_Settings.Name} {ScenarioFactory.RemoteNodeConstants.k_AllocateNodePostfix}");
            executionGraph.AddNode(allocateNode, ExecutionStage.Run);
            executionGraph.ConnectConstant(allocateNode.FleetName, fleetName);
            executionGraph.ConnectConstant(allocateNode.BuildConfigurationName, buildConfigurationName);

            var remoteRunNode = new RunServerNode($"{m_Settings.Name} {ScenarioFactory.RemoteNodeConstants.k_RunNodePostfix}");
            executionGraph.AddNode(remoteRunNode, ExecutionStage.Run);
            executionGraph.ConnectConstant(remoteRunNode.StreamLogs, m_Settings.AdvancedConfiguration.StreamLogsToMainEditor);
            executionGraph.ConnectConstant(remoteRunNode.LogsColor, m_Settings.AdvancedConfiguration.LogsColor);
            executionGraph.Connect(allocateNode.ServerId, remoteRunNode.ServerId);
            executionGraph.Connect(allocateNode.ConnectionDataOut, remoteRunNode.ConnectionData);

            // [TODO]: We need to remove this line, since 1 instance could have multiple nodes
            m_Settings.CorrespondingNodeId = remoteRunNode.Name;

            m_Settings.SetCorrespondingNodes(buildNode, deployBuildNode, deployBuildConfigNode, deployFleetNode, allocateNode, remoteRunNode);
        }

        /// <summary>
        /// Validates whether the current Unity project is properly set up for running a remote instance
        /// This includes:
        /// 1. Checking if the project is linked to Unity Cloud (i.e., has a valid cloud project ID)
        /// 2. Verifying the existence of a valid Unity Cloud Environment ID
        /// 3. Confirming Multiplay is connected and ready via the Multiplay Provider API
        /// Returns a failed ValidationResult if any step is not properly configured.
        /// </summary>
        protected internal override async Task<Scenario.ValidationResult> ValidateForRunningAsync(CancellationToken cancellationToken)
        {
            // Check if the project is linked to Unity Cloud, if linked, cloud project ID is not null
            if (!MultiplayValidationHelper.IsProjectLinkedToUnityCloud())
            {
                MultiplayUtils.LogCloudSetupErrors();
                return new Scenario.ValidationResult(false, "Project is not connected to Unity Cloud");
            }

            try
            {
                var validateRemoteResult = await MultiplayValidationHelper.ValidateRemoteAsync(cancellationToken);
                return await MultiplayValidationHelper.ValidateMultiplayProviderStatusAsync(validateRemoteResult, cancellationToken);
            }
            // Catching the specific exception for environment setup
            catch (MultiplayEnvironmentException)
            {
                MultiplayUtils.LogEnvironmentSetupErrors();
                MultiplayUtils.LogMultiplaySetupErrors(isProjectDashboardLinkAvailable: true);
                return new Scenario.ValidationResult(false, "Unity Cloud environment not specified");
            }
            // Catching the specific exception for service token issues
            catch (MultiplayUdashServiceTokenException)
            {
                MultiplayUtils.LogServiceTokenErrors();
                return new Scenario.ValidationResult(false, "Failed to get Unity Cloud service token");
            }
        }
    }
}
#endif
