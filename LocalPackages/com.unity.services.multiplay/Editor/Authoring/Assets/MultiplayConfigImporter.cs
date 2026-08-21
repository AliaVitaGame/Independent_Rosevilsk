using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Unity.Services.Multiplay.Authoring.Editor.Assets
{
    [ScriptedImporter(k_Version, MultiplayConfigResource.FileExtension)]
    class MultiplayConfigImporter : ScriptedImporter
    {
        const int k_Version = 1;

        public void OnValidate()
        {
            hideFlags = HideFlags.HideInInspector;
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var asset = ScriptableObject.CreateInstance<MultiplayConfigAsset>();

            var configContent = File.ReadAllText(ctx.assetPath);
            asset.Path = ctx.assetPath;
            asset.FromYaml(configContent);

            ctx.AddObjectToAsset("MainAsset", asset, MultiplayConfigResource.Icon);
            ctx.SetMainObject(asset);
        }
    }
}
