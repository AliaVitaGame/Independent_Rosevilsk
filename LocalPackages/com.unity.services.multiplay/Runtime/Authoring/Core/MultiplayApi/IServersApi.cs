using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Services.Multiplay.Authoring.Core.MultiplayApi
{
    /// <summary>
    /// Provides access to server information for the project.
    /// See https://services.docs.unity.com/multiplay-config/v1/#tag/Servers for details
    /// </summary>
    public interface IServersApi : IInitializable
    {
        /// <summary>
        /// Trigger an action against the server with the given id
        /// </summary>
        /// <param name="serverId">The server Id</param>
        /// <param name="action">The server action to trigger</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task embedding a boolean that is true when the task was successfully triggered</returns>
        Task<bool> TriggerServerActionAsync(long serverId, ServerAction action, CancellationToken cancellationToken = default);
        /// <summary>
        /// Get the details of a single server with the given ID
        /// </summary>
        /// <param name="serverId">The server Id</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task embedding the server info</returns>
        Task<ServerInfo> GetServerAsync(long serverId, CancellationToken cancellationToken = default);
        /// <summary>
        /// Lists the Action Logs for a server
        /// </summary>
        /// <param name="serverId">The server Id</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task embedding the action logs</returns>
        Task<List<ActionLog>> GetServerActionLogsAsync(long serverId, CancellationToken cancellationToken = default);
    }
}
