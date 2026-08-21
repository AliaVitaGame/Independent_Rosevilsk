using System;
using System.Collections.Generic;

namespace Unity.Services.Multiplay.Authoring.Core.MultiplayApi
{
    /// <summary>
    /// Information of the fleet
    /// </summary>
    public class FleetInfo
    {
        internal FleetInfo(
            string fleetName,
            FleetId id,
            Status fleetStatus,
            Guid osId,
            string osName,
            List<FleetRegionInfo> regions,
            AllocationStatus allocationStatus = null,
            List<BuildConfigInfo> buildConfigInfos = null)
        {
            FleetName = fleetName;
            FleetStatus = fleetStatus;
            OsId = osId;
            OSName = osName;
            Regions = regions;
            Id = id;
            Allocation = allocationStatus;
            BuildConfigInfos = buildConfigInfos;
        }

        /// <summary>
        /// Id of the fleet
        /// </summary>
        public FleetId Id { get; }
        /// <summary>
        /// Name of the fleet
        /// </summary>
        public string FleetName { get; }
        /// <summary>
        /// Status of the fleet
        /// </summary>
        public Status FleetStatus { get; }
        /// <summary>
        /// Associated OsId
        /// </summary>
        internal Guid OsId { get; }
        internal string OSName { get; }

        /// <summary>
        /// Regions the fleet has access to
        /// </summary>
        public List<FleetRegionInfo> Regions { get; }
        /// <summary>
        /// Fleet allocation information
        /// </summary>
        public AllocationStatus Allocation { get; }

        /// <summary>
        /// Information about the build configurations
        /// </summary>
        public List<BuildConfigInfo> BuildConfigInfos { get; }

        /// <summary>
        /// Status of the fleet
        /// </summary>
        public enum Status
        {
            /// <summary>
            /// Fleet is online
            /// </summary>
            Online = 1,
            /// <summary>
            /// Fleet is draining
            /// </summary>
            Draining = 2,
            /// <summary>
            /// Fleet is offline
            /// </summary>
            Offline = 3
        }

        /// <summary>
        /// The total allocation status
        /// </summary>
        /// <param name="Total">Total number of servers</param>
        /// <param name="Allocated">Number of Servers allocated</param>
        /// <param name="Available">Number of Servers available</param>
        /// <param name="Online">Number of Servers online</param>
        public record AllocationStatus(int Total, int Allocated, int Available, int Online);

        /// <summary>
        /// Information of fleet region
        /// </summary>
        /// <param name="Id">Fleet region ID</param>
        /// <param name="RegionId">Region ID</param>
        /// <param name="Name">Name of associated region</param>
        public record FleetRegionInfo(Guid Id, Guid RegionId, string Name);

        /// <summary>
        /// Information of a BuildConfig
        /// </summary>
        /// <param name="Id">BuildConfig ID</param>
        /// <param name="Name">Name of associated BuildConfig</param>
        public record BuildConfigInfo(long Id, string Name);
    }
}
