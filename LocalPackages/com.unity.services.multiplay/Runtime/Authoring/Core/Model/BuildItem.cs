using System;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Multiplay.Authoring.Core.Assets;

namespace Unity.Services.Multiplay.Authoring.Core.Model
{
    /// <summary>
    /// Represents a deployable Build object
    /// </summary>
    [Serializable]
    public class BuildItem : DeploymentItem, IResourceRef
    {
        /// <summary>
        /// Creates a new instance of the BuildItem
        /// </summary>
        public BuildItem()
        {
            Type = "Build";
        }
        /// <summary>
        /// The name of the underlying Build Configuration
        /// </summary>
        public BuildName OriginalName { get; set; }
        /// <summary>
        /// The definition of the Build
        /// </summary>
        public MultiplayConfig.BuildDefinition Definition { get; set; }
        /// <summary>
        /// The name of the underlying Build Configuration
        /// </summary>
        public IResourceName ResourceName => OriginalName;
    }
}
