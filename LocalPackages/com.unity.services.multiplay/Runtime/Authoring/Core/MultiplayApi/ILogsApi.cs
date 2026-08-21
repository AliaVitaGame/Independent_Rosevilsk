using System.Threading;
using System.Threading.Tasks;

namespace Unity.Services.Multiplay.Authoring.Core.MultiplayApi
{
    /// <summary>
    /// Allows to find logs happening on the remote server
    /// </summary>
    public interface ILogsApi : IInitializable
    {
        /// <summary>
        /// Returns logs found according to the parameters
        /// </summary>
        /// <param name="searchParams">The search parameters</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>Returns a Task containing the result that resolves once the search is complete</returns>
        Task<LogSearchResult> SearchLogsAsync(LogSearchParams searchParams, CancellationToken cancellationToken = default);
    }
}
