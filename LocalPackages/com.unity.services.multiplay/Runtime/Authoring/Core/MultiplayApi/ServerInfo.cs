namespace Unity.Services.Multiplay.Authoring.Core.MultiplayApi
{
    /// <summary>
    /// Status of the server.
    /// </summary>
    public enum ServerStatus
    {
        /// <summary>
        /// Server status is allocated
        /// </summary>
        ALLOCATED = 1,
        /// <summary>
        /// Server is reserved
        /// </summary>
        RESERVED = 2,
        /// <summary>
        /// Server is available
        /// </summary>
        AVAILABLE = 3,
        /// <summary>
        /// Server is online
        /// </summary>
        ONLINE = 4,
        /// <summary>
        /// Server is ready
        /// </summary>
        READY = 5,
        /// <summary>
        /// Server is held
        /// </summary>
        HELD = 6
    }

    /// <summary>
    /// Information regarding the server
    /// </summary>
    /// <param name="Id">ID of the server.</param>
    /// <param name="MachineID">ID of the machine hosting the server.</param>
    /// <param name="MachineName">Name of the machine hosting the server.</param>
    /// <param name="BuildConfigurationID">ID of the associated build configuration.</param>
    /// <param name="BuildConfigurationName">Name of the associated build configuration.</param>
    /// <param name="BuildName">Name of the build for the associated build configuration.</param>
    /// <param name="FleetID">ID of the associated fleet.</param>
    /// <param name="FleetName">Name of the associated fleet.</param>
    /// <param name="LocationID">ID of the associated location.</param>
    /// <param name="LocationName">Name of the associated location.</param>
    /// <param name="Ip">IP address of the server.</param>
    /// <param name="Port">Network port the server is running on.</param>
    /// <param name="Status">Status of the server.</param>
    /// <param name="CpuLimit">CPU Speed Limit (MHz) of the server.</param>
    /// <param name="MemoryLimit">Memory (RAM) (MiB) Limit of the server.</param>
    /// <param name="Deleted">Whether the server is marked as deleted.</param>
    /// <param name="HoldExpiresAt">The unix timestamp, in seconds, at which a held server automatically expires and releases its hold.</param>
    public record ServerInfo(long Id, long MachineID, string MachineName, long BuildConfigurationID, string BuildConfigurationName, string BuildName, System.Guid FleetID, string FleetName, long LocationID, string LocationName, string Ip, int Port, ServerStatus Status, long CpuLimit, long MemoryLimit, bool Deleted, long HoldExpiresAt = default);
}
