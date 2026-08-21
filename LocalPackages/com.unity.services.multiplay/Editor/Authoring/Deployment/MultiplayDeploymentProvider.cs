using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Multiplay.Authoring.Editor.Assets;
using Unity.Services.Multiplay.Authoring.Editor.Shared.Collections;

namespace Unity.Services.Multiplay.Authoring.Editor.Deployment
{
    class MultiplayDeploymentProvider : DeploymentProvider
    {
        const string k_BuildExt = ".build";
        const string k_BuildConfigExt = ".buildConfig";
        const string k_FleetExt = ".fleet";

        public override string Service => "Multiplay";
        public override Command DeployCommand { get; }

        readonly IItemStore m_ItemStore;

        public MultiplayDeploymentProvider(
            ObservableCollection<MultiplayConfigAsset> assets,
            Command deployCommand,
            IItemStore itemStore)
        {
            DeployCommand = deployCommand;
            m_ItemStore = itemStore;

            assets.CollectionChanged += AssetsOnCollectionChanged;
            foreach (var asset in assets)
            {
                asset.PropertyChanged += UpdateConfigAsset;
                AddConfigAsset(asset);
            }
        }

        void AssetsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (var asset in e.OldItems?.Cast<MultiplayConfigAsset>() ?? Array.Empty<MultiplayConfigAsset>())
            {
                asset.PropertyChanged -= UpdateConfigAsset;
                RemoveConfigAsset(asset);
                m_ItemStore.RemoveConfig(asset);
            }

            foreach (var asset in e.NewItems?.Cast<MultiplayConfigAsset>() ?? Array.Empty<MultiplayConfigAsset>())
            {
                asset.PropertyChanged += UpdateConfigAsset;
                AddConfigAsset(asset);
            }
        }

        void AddConfigAsset(MultiplayConfigAsset asset)
        {
            asset.Config?.Builds?.ForEach(kv =>
            {
                var name = kv.Key;
                var build = kv.Value;
                var item = m_ItemStore.GetOrCreate(asset, name);

                item.Name = name + k_BuildExt;
                item.OriginalName = name;
                item.Path = asset.Path;
                item.Definition = build;

                DeploymentItems.Add(item);
            });

            asset.Config?.BuildConfigurations?.ForEach(kv =>
            {
                var name = kv.Key;
                var configuration = kv.Value;
                var item = m_ItemStore.GetOrCreate(asset, name);

                item.Name = name + k_BuildConfigExt;
                item.OriginalName = name;
                item.Path = asset.Path;
                item.Definition = configuration;

                DeploymentItems.Add(item);
            });

            asset.Config?.Fleets?.ForEach(kv =>
            {
                var name = kv.Key;
                var fleet = kv.Value;
                var item = m_ItemStore.GetOrCreate(asset, name);

                item.Name = name + k_FleetExt;
                item.OriginalName = name;
                item.Path = asset.Path;
                item.Definition = fleet;

                DeploymentItems.Add(item);
            });
        }

        void RemoveConfigAsset(MultiplayConfigAsset asset)
        {
            foreach (var item in m_ItemStore.ItemsForConfig(asset))
            {
                DeploymentItems.Remove(item);
            }
        }

        void UpdateConfigAsset(object sender, PropertyChangedEventArgs e)
        {
            var asset = (MultiplayConfigAsset)sender;
            RemoveConfigAsset(asset);
            AddConfigAsset(asset);
        }
    }
}
