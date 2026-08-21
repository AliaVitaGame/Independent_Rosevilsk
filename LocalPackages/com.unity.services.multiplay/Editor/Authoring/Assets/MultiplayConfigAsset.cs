using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Unity.Services.Multiplay.Authoring.Core.Assets;
using Unity.Services.Multiplay.Authoring.Editor.Shared.Assets;
using Unity.Services.Multiplay.Authoring.Editor.Shared.Threading;
using Unity.Services.Multiplay.Authoring.YamlDotNet.Serialization;
using Unity.Services.Multiplay.Authoring.YamlDotNet.Serialization.NamingConventions;
using UnityEngine;
using Logger = Unity.Services.Multiplay.Authoring.Editor.Shared.Logging.Logger;

namespace Unity.Services.Multiplay.Authoring.Editor.Assets
{
    class MultiplayConfigAsset : ScriptableObject, ISerializationCallbackReceiver, IPath, INotifyPropertyChanged
    {
        public MultiplayConfig Config
        {
            get => m_Config;
            set
            {
                m_Config = value;
                NormalizeBuildPaths();
                OnPropertyChanged(nameof(Config));
            }
        }

        public string Path
        {
            get => m_Path;
            set
            {
                m_Path = value;
                NormalizeBuildPaths();
                OnPropertyChanged(nameof(Path));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [SerializeField]
        string configBacking;
        [NonSerialized]
        MultiplayConfig m_Config;
        [NonSerialized]
        string m_Path;

        public string ToYaml()
        {
            var serializer = new SerializerBuilder()
                .WithTypeConverter(new ResourceNameTypeConverter())
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            return serializer.Serialize(Config);
        }

        public void FromYaml(string yaml)
        {
            try
            {
                var deserializer = new DeserializerBuilder()
                    .WithTypeConverter(new ResourceNameTypeConverter())
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                Config = deserializer.Deserialize<MultiplayConfig>(yaml);
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                Config = null;
            }
        }

        public void OnBeforeSerialize()
        {
            configBacking = ToYaml();
        }

        public void OnAfterDeserialize()
        {
            FromYaml(configBacking);
        }

        void NormalizeBuildPaths()
        {
            if (Config?.Builds == null || Path == null)
            {
                return;
            }

            var basePath = System.IO.Path.GetFullPath(System.IO.Path.GetDirectoryName(Path) !);
            foreach (var(_, definition) in Config.Builds)
            {
                if (definition.BuildPath != null && definition.BuildPath.StartsWith("."))
                {
                    definition.BuildPath = System.IO.Path.GetFullPath(definition.BuildPath, basePath);
                }
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            Sync.RunNextUpdateOnMain(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
        }
    }
}
