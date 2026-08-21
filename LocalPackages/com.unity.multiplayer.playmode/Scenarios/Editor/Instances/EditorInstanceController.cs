using Unity.Multiplayer.PlayMode.Scenarios.Editor.Api;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.Nodes.Editor;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor.Instances
{
    class EditorInstanceController : PlayModeController
    {
        private readonly EditorInstanceDescription m_Settings;

        internal EditorInstanceController(EditorInstanceDescription editorInstanceDescription)
        {
            m_Settings = editorInstanceDescription;
        }

        protected internal override void SetupExecutionGraph(ExecutionGraph executionGraph)
        {
            var editorRunNode = new EditorMultiplayerPlaymodeRunNode($"{m_Settings.Name}|{m_Settings.PlayerInstanceIndex}_run");
            var deployNode = new EditorMultiplayerPlaymodeDeployNode($"{m_Settings.Name}|{m_Settings.PlayerInstanceIndex}_deploy");

            executionGraph.AddNode(deployNode, ExecutionStage.Deploy);
            executionGraph.ConnectConstant(deployNode.PlayerInstanceIndex, m_Settings.PlayerInstanceIndex);
            executionGraph.ConnectConstant(deployNode.PlayerTags, m_Settings.PlayerTag);
#if UNITY_USE_MULTIPLAYER_ROLES
            executionGraph.ConnectConstant(deployNode.MultiplayerRole, m_Settings.RoleMask);
#endif
            executionGraph.ConnectConstant(deployNode.InitialScene, m_Settings.InitialScene);

            // [TODO]: We need to remove this line, since 1 instance could have multiple nodes
            m_Settings.CorrespondingNodeId = editorRunNode.Name;
            m_Settings.SetCorrespondingNodes(editorRunNode, deployNode);

            executionGraph.AddNode(editorRunNode, ExecutionStage.Run);
            executionGraph.ConnectConstant(editorRunNode.PlayerInstanceIndex, m_Settings.PlayerInstanceIndex);
            executionGraph.ConnectConstant(editorRunNode.PlayerTags, m_Settings.PlayerTag);

            if (m_Settings is VirtualEditorInstanceDescription virtualEditorInstanceDescription)
            {
                executionGraph.ConnectConstant(editorRunNode.StreamLogs, virtualEditorInstanceDescription.AdvancedConfiguration.StreamLogsToMainEditor);
                executionGraph.ConnectConstant(editorRunNode.LogsColor, virtualEditorInstanceDescription.AdvancedConfiguration.LogsColor);
            }
        }
    }
}
