using System;

namespace Unity.Services.Multiplay.Authoring.Core.MultiplayApi
{
    /// <summary>
    /// Represents the search params for a log
    /// </summary>
    /// <param name="FleetId">The fleet Id</param>
    /// <param name="ServerId">The server Id</param>
    /// <param name="Query">Terms to search for in log events</param>
    /// <param name="Size">Limits the number of results returned</param>
    /// <param name="From">Start date-time of log entries to query</param>
    /// <param name="To">End date-time of log entries to query</param>
    /// <param name="PaginationToken">A token used to fetch the next page of results. If provided, all other fields are ignored</param>
    public record LogSearchParams(Guid FleetId, long ServerId, string Query, int Size, DateTime From, DateTime To, string PaginationToken = null);
}
