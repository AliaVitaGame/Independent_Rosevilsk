#if UNITY_USE_MULTIPLAYER_ROLES
using Unity.Multiplayer.Editor;
#endif
using System.Collections.Generic;
using System.Linq;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation;
using UnityEngine.Assertions;
using UnityEditor.Build.Profile;
using System.Text.RegularExpressions;
using Unity.Multiplayer.PlayMode.Configurations.Editor;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.Api;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.Instances;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor
{
    /// <summary>
    /// Creates a scenario graph from a list of instance descriptions.
    /// </summary>
    internal static class ScenarioFactory
    {
#if UNITY_USE_MULTIPLAYER_ROLES
        public static MultiplayerRoleFlags GetRoleForInstance(InstanceDescription instance)
        {
            Assert.IsNotNull(instance, $"Null instance used");
            switch (instance)
            {
                case EditorInstanceDescription editorInstance: return editorInstance.RoleMask;
                case IBuildableInstanceDescription buildableInstance: return buildableInstance.BuildProfile == null ? MultiplayerRoleFlags.Client : EditorMultiplayerRolesManager.GetMultiplayerRoleForBuildProfile(buildableInstance.BuildProfile);
            }
            return MultiplayerRoleFlags.Client;
        }
#endif
        internal static class RemoteNodeConstants
        {
            internal const string k_BuildNodePostFix = "- Build";
            internal const string k_DeployBuildNodePostfix = "- Deploy Build";
            internal const string k_DeployConfigBuildNodePostfix = "- Deploy Build Configuration";
            internal const string k_DeployFleetNodePostfix = "- Deploy Fleet";
            internal const string k_RunNodePostfix = "- Run";
            internal const string k_AllocateNodePostfix = "- Allocate";
        }

        private static void CategorizeInstances(
            List<InstanceDescription> instances,
            out List<InstanceDescription> servers,
            out List<InstanceDescription> clients)
        {
            servers = new List<InstanceDescription>();
            clients = new List<InstanceDescription>();

            foreach (var instance in instances)
            {
#if UNITY_USE_MULTIPLAYER_ROLES
                if (GetRoleForInstance(instance).HasFlag(MultiplayerRoleFlags.Server))
                    servers.Add(instance);
                else
#endif
                {
                    clients.Add(instance);
                }
            }
        }

        public static Scenario CreateScenario(string name, List<InstanceDescription> instanceDescriptions)
        {
            var scenario = Scenario.Create(name);

            CategorizeInstances(instanceDescriptions, out var serverDescriptList, out var clientDescriptList);

            // [TODO] It's good to report the error if multiple servers are selected but we also need to report it earlier, directly in the configuration UI.
            Assert.IsTrue(serverDescriptList.Count() <= 1, "There can only be one server in a scenario");

            // This will ensure the server instance is the first to be added in the scenario.
            var serverDescript = serverDescriptList.FirstOrDefault();
            if (serverDescript != null)
            {
                var serverInstance = ConnectOrCreateInstance(serverDescript);
                scenario.AddInstance(serverInstance);
            }

            // Finally, iterate through the rest of the instance descriptions, construct client instances
            // and configure the connection data for when they run.
            foreach (var clientDescript in clientDescriptList)
            {
                var clientInstance = ConnectOrCreateInstance(clientDescript);
                scenario.AddInstance(clientInstance);
            }
            return scenario;
        }

        private static Instance ConnectOrCreateInstance(InstanceDescription instanceDescription)
        {
            // If an Existing Instance is Actively Free Running, we connect that instance to this new Scenario.
            if (PlayModeManager.instance.ActivePlayModeConfig is ScenarioConfig config &&
                config.Scenario != null)
            {
                var activeFreeInstance = config.Scenario.GetInstanceByName(instanceDescription.Name, true);
                if (activeFreeInstance != null)
                {
                    activeFreeInstance.GetExecutionGraph().GetNodes(ExecutionStage.Run);

                    // Ensure we remove it from the old Scenario
                    config.Scenario.RemoveInstance(activeFreeInstance);

                    // Finally return the Free run instance to be attached to the new one.
                    return activeFreeInstance;
                }
            }

            // Else, create and rebuild the instance from the description as per usual.
            return CreateInstance(instanceDescription);
        }

        private static Instance CreateInstance(InstanceDescription instanceDescription)
        {
            if (instanceDescription is EditorInstanceDescription editorDescription)
            {
                return CreateEditorInstance(editorDescription);
            }
            if (instanceDescription is LocalInstanceDescription localDescription)
            {
                return CreateLocalInstance(localDescription);
            }
            if (instanceDescription is RemoteInstanceDescription remoteDescription)
            {
#if MULTIPLAY_API_AVAILABLE
                return CreateRemoteInstance(remoteDescription);
#else
                throw new System.Exception("The Multiplay API is not available. It is not possible to create a InsertRemoteInstance instance without it.");
#endif
            }
            throw new System.NotImplementedException();
        }

        static Instance CreateEditorInstance(EditorInstanceDescription editorInstanceDescription)
        {
            var editorController = new EditorInstanceController(editorInstanceDescription);
            var instance = Instance.Create(editorInstanceDescription, editorController);
            var executionGraph = instance.GetExecutionGraph();

            editorController.SetupExecutionGraph(executionGraph);
            return instance;
        }

        private static Instance CreateLocalInstance(LocalInstanceDescription description)
        {
            var localController = new LocalInstanceController(description);
            var instance = Instance.Create(description, localController);
            var executionGraph = instance.GetExecutionGraph();

            localController.SetupExecutionGraph(executionGraph);
            return instance;
        }

        private static Instance CreateRemoteInstance(RemoteInstanceDescription description)
        {
#if !MULTIPLAY_API_AVAILABLE
            throw new System.Exception("The Multiplay API is not available. It is not possible to create the corresponding Deploy and Run nodes without it.");
#else
            var remoteController = new RemoteInstanceController(description);
            var instance = Instance.Create(description, remoteController);
            var executionGraph = instance.GetExecutionGraph();

            remoteController.SetupExecutionGraph(executionGraph);
            return instance;
#endif
        }

        internal static string GenerateBuildPath(BuildProfile profile)
        {
            // It is important that all builds are in its own folder because when we upload the build to the Multiplay service,
            // we upload the whole folder. If we have multiple builds in the same folder, we will upload all of them.
            var escapedProfileName = EscapeProfileName(profile.name);
            return $"Builds/PlayModeScenarios/{escapedProfileName}/{escapedProfileName}";
        }

        private static string EscapeProfileName(string path) => Regex.Replace(path, @"[^\w\d]", "_");
    }
}
