using System;

namespace Unity.Services.Multiplay.Authoring.Core.MultiplayApi
{
    /// <summary>
    /// Represents a type-safe Fleet ID
    /// </summary>
    public struct FleetId
    {
        /// <summary>
        /// The numerical value of the Fleet
        /// </summary>
        public Guid Guid { get; init; }
    }
}
