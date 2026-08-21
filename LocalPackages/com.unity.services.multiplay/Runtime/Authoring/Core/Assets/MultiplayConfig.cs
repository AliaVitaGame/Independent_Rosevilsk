using System.Collections.Generic;

namespace Unity.Services.Multiplay.Authoring.Core.Assets
{
    /// <summary>
    /// Represents the model of a Multiplay configuration file
    /// </summary>
    public class MultiplayConfig
    {
        /// <summary>
        /// Version of the multiplay schema
        /// </summary>
        public string Version { get; init; }
        /// <summary>
        /// Build definitions for this configuration
        /// </summary>
        public IDictionary<BuildName, BuildDefinition> Builds { get; init; } = new Dictionary<BuildName, BuildDefinition>();
        /// <summary>
        /// Build Config definitions for this configuration
        /// </summary>
        public IDictionary<BuildConfigurationName, BuildConfigurationDefinition> BuildConfigurations { get; init; } = new Dictionary<BuildConfigurationName, BuildConfigurationDefinition>();
        /// <summary>
        /// Fleet definitions for this configuration
        /// </summary>
        public IDictionary<FleetName, FleetDefinition> Fleets { get; init; } = new Dictionary<FleetName, FleetDefinition>();

        /// <summary>
        /// Represents a Build definition
        /// </summary>
        public class BuildDefinition
        {
            /// <summary>
            /// The path to the executable of the build
            /// </summary>
            public string ExecutableName { get; init; } = "Server";
            /// <summary>
            /// The path to the Build location
            /// </summary>
            public string BuildPath { get; set; }
        }

        /// <summary>
        /// Represents the definition of a build configuration
        /// </summary>
        public class BuildConfigurationDefinition
        {
            /// <summary>
            /// The name of the Build that the Build Configuration uses
            /// </summary>
            public BuildName Build { get; init; }
            /// <summary>
            /// The query type for the build configuration
            /// </summary>
            public Query? QueryType { get; init; }
            /// <summary>
            /// The path to the binary from the build
            /// </summary>
            public string BinaryPath { get; init; }
            /// <summary>
            /// The command line this build configuration should use for the associated build
            /// </summary>
            public string CommandLine { get; init; }
            /// <summary>
            /// Additional environment variables to use with this build configuration
            /// </summary>
            public IDictionary<string, string> Variables { get; init; } = new Dictionary<string, string>();
            /// <summary>
            /// Deprecated - The number of cores that should be used for this build configuration
            /// </summary>
            public int Cores { get; init; } = 1;
            /// <summary>
            /// Deprecated - The CPU utilisation per core.
            /// </summary>
            public int SpeedMhz { get; init; } = 750;
            /// <summary>
            /// Deprecated - Memory required per server.
            /// </summary>
            public int MemoryMiB { get; init; } = 800;
        }

        /// <summary>
        /// Definition of a fleet
        /// </summary>
        public class FleetDefinition
        {
            /// <summary>
            /// A list of region associations and scaling settings with the fleet
            /// </summary>
            public IDictionary<string, ScalingDefinition> Regions { get; init; } = new Dictionary<string, ScalingDefinition>();
            /// <summary>
            /// A list of build configuration to associate with the fleet.
            /// </summary>
            public IList<BuildConfigurationName> BuildConfigurations { get; init; } = new List<BuildConfigurationName>();
        }

        /// <summary>
        /// Scaling settings for a fleet
        /// </summary>
        public class ScalingDefinition
        {
            /// <summary>
            /// Minimum number of servers to keep free for new game sessions.
            /// </summary>
            public int MinAvailable { get; init; }
            /// <summary>
            /// Maximum number of servers to host in the fleet region
            /// </summary>
            public int MaxServers { get; init; }

            /// <summary>
            /// Is the Region online
            /// </summary>
            public bool Online { get; init; } = true;
        }

        /// <summary>
        /// Available query protocols: https://docs.unity.com/ugs/en-us/manual/game-server-hosting/manual/concepts/query-protocols
        /// </summary>
        public enum Query
        {
            /// <summary>
            /// The Server Query Protocol (SQP) allows clients to retrieve information about a running game server using UDP/IP packets.
            /// </summary>
            Sqp,
            /// <summary>
            /// A2S is a UDP-based game server query protocol that Valve Software keeps as a part of the Steam SDK.
            /// </summary>
            A2s,
            /// <summary>
            /// No query protocol
            /// </summary>
            None
        }
    }
}
