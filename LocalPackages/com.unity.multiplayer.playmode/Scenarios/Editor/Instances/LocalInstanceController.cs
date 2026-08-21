using System.Text.RegularExpressions;
using Unity.Multiplayer.PlayMode.Editor.Bridge;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.Api;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.Nodes.Editor;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.Nodes.Local;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.Nodes.Local.Unity.Multiplayer.PlayMode.Scenarios.Editor.Nodes.Local;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor.Instances
{
    class LocalInstanceController : PlayModeController
    {
        private readonly LocalInstanceDescription m_Settings;

        internal LocalInstanceController(LocalInstanceDescription localInstanceDescription)
        {
            m_Settings = localInstanceDescription;
        }

        protected internal override void SetupExecutionGraph(ExecutionGraph executionGraph)
        {
            // TODO: We need to share the build nodes between instances that share the same build profile and role.
            var buildNode = new EditorBuildNode($"{m_Settings.Name} - Build");
            executionGraph.AddNode(buildNode, ExecutionStage.Prepare);

            executionGraph.ConnectConstant(buildNode.BuildPath, ScenarioFactory.GenerateBuildPath(m_Settings.BuildProfile));
            executionGraph.ConnectConstant(buildNode.Profile, m_Settings.BuildProfile);

            var deviceRunNode = new LocalDeviceRunNode($"{m_Settings.Name} - Run");

            // TODO: UUM-50144 - There is currently a bug in windows dedicated server where screen related
            // arguments cause a crash. As a temporary workaround we detect that case and remove any
            // of those arguments that, in any case, take no effect on that platform.
            var arguments = m_Settings.AdvancedConfiguration.Arguments;
            if (InternalUtilities.IsServerProfile(m_Settings.BuildProfile))
                arguments = CleanupScreenArguments(arguments);

            if (InternalUtilities.IsAndroidBuildTarget(m_Settings.BuildProfile))
            {
                executionGraph.AddNode(deviceRunNode, ExecutionStage.Run);
                executionGraph.ConnectConstant(deviceRunNode.Arguments, arguments);
                executionGraph.ConnectConstant(deviceRunNode.StreamLogs, m_Settings.AdvancedConfiguration.StreamLogsToMainEditor);
                executionGraph.ConnectConstant(deviceRunNode.LogsColor, m_Settings.AdvancedConfiguration.LogsColor);
                executionGraph.ConnectConstant(deviceRunNode.DeviceName, m_Settings.AdvancedConfiguration.DeviceID);

                executionGraph.Connect(buildNode.ExecutablePath, deviceRunNode.ExecutablePath);
                executionGraph.Connect(buildNode.BuildReport, deviceRunNode.BuildReport);

                // [TODO]: We need to remove this line, since 1 instance could have multiple nodes
                m_Settings.CorrespondingNodeId = deviceRunNode.Name;

                m_Settings.SetCorrespondingNodes(buildNode, deviceRunNode);
                return;
            }

            var localRunNode = new LocalRunNode($"{m_Settings.Name} - Run");
            executionGraph.AddNode(localRunNode, ExecutionStage.Run);

            executionGraph.ConnectConstant(localRunNode.Arguments, arguments);
            executionGraph.ConnectConstant(localRunNode.StreamLogs, m_Settings.AdvancedConfiguration.StreamLogsToMainEditor);
            executionGraph.ConnectConstant(localRunNode.LogsColor, m_Settings.AdvancedConfiguration.LogsColor);
            executionGraph.Connect(buildNode.ExecutablePath, localRunNode.ExecutablePath);

            // [TODO]: We need to remove this line, since 1 instance could have multiple nodes
            m_Settings.CorrespondingNodeId = localRunNode.Name;

            m_Settings.SetCorrespondingNodes(buildNode, localRunNode);
        }

        private static string CleanupScreenArguments(string arguments)
        {
            // We need to remove -screen-fullscreen -screen-width and -screen-height arguments
            arguments = Regex.Replace(arguments, @"-screen-fullscreen\s+\d*", "");
            arguments = Regex.Replace(arguments, @"-screen-width\s+\d*", "");
            arguments = Regex.Replace(arguments, @"-screen-height\s+\d*", "");
            return arguments;
        }
    }
}
