using System.Collections.Generic;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Multiplay.Authoring.Core.Assets;
using Unity.Services.Multiplay.Authoring.Core.Model;
using Unity.Services.Multiplay.Authoring.Editor.Assets;

namespace Unity.Services.Multiplay.Authoring.Editor.Deployment
{
    interface IItemStore
    {
        IReadOnlyList<DeploymentItem> GetAllItems();
        BuildItem GetOrCreate(MultiplayConfigAsset config, BuildName name);
        BuildConfigurationItem GetOrCreate(MultiplayConfigAsset config, BuildConfigurationName name);
        FleetItem GetOrCreate(MultiplayConfigAsset config, FleetName name);
        IEnumerable<DeploymentItem> ItemsForConfig(MultiplayConfigAsset config);
        void RemoveConfig(MultiplayConfigAsset config);
    }
}
