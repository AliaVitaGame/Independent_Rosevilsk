using System;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Multiplay.Authoring.Core.Assets;

namespace Unity.Services.Multiplay.Authoring.Core.Model
{
    /// <summary>
    /// Represents a deployable Fleet object
    /// </summary>
    [Serializable]
    public class
        FleetItem : DeploymentItem, IResourceRef
    {
        /// <summary>
        /// Create a new instance of a FleetItem
        /// </summary>
        public FleetItem()
        {
            Type = "Fleet";
        }

        /// <summary>
        /// The name of the underlying Fleet
        /// </summary>
        public FleetName OriginalName { get; set; }

        /// <summary>
        /// The definition of the Build Configuration
        /// </summary>
        public MultiplayConfig.FleetDefinition Definition { get; set; }
        /// <summary>
        /// The name of the underlying Fleet
        /// </summary>
        public IResourceName ResourceName => OriginalName;
    }
}
