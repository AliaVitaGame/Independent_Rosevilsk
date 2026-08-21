using System.IO;
using Unity.Services.Multiplay.Authoring.Core.Assets;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace Unity.Services.Multiplay.Authoring.Editor.Assets
{
    class CreateMultiplayConfigMenu : AssetCreationEndAction
    {
        const string k_DefaultName = "new_multiplay_config";
        const int k_CreateItemPriority = 82;

        [MenuItem("Assets/Create/Multiplay Config", false, k_CreateItemPriority)]
        public static void CreateMultiplayConfigFile()
        {
            var filePath = k_DefaultName + MultiplayConfigResource.FileExtension;
            var icon = MultiplayConfigResource.Icon;

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                EntityId.None,
                CreateInstance<CreateMultiplayConfigMenu>(),
                filePath,
                icon,
                null);
        }

        [InitializeOnLoadMethod]
        static void SetMonoDefinitionIcon()
        {
            var monoImporter = (MonoImporter)AssetImporter.GetAtPath(MultiplayConfigResource.MonoDefinitionPath);
            var monoScript = monoImporter.GetScript();
            EditorGUIUtility.SetIconForObject(monoScript,  MultiplayConfigResource.Icon);
        }

        public override void Action(EntityId entityId, string pathName, string resourceFile)
        {
            File.WriteAllText(pathName, MultiplayConfigTemplate.Yaml);
            AssetDatabase.Refresh();
        }
    }
}
