using System;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Multiplay.Authoring.Core.Assets;

namespace Unity.Services.Multiplay.Authoring.Core.Model
{
    /// <summary>
    /// Represents a Build Configuration deployable item
    /// </summary>
    [Serializable]
    public class BuildConfigurationItem : DeploymentItem, IResourceRef
    {
        /// <summary>
        /// Create a new instance of a Build Configuration Item. TBA
        /// </summary>
        public BuildConfigurationItem()
        {
            Type = "Build Configuration";
        }

        /// <summary>
        /// The name of the underlying Build Configuration
        /// </summary>
        public BuildConfigurationName OriginalName { get; set; }
        /// <summary>
        /// The definition of the Build Configuration
        /// </summary>
        public MultiplayConfig.BuildConfigurationDefinition Definition { get; set; }

        /// <summary>
        /// The name of the underlying Build Configuration
        /// </summary>
        public IResourceName ResourceName => OriginalName;
    }
}
