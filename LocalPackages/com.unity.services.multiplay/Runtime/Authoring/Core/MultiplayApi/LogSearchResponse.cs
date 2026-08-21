using System;
using System.Collections.Generic;

namespace Unity.Services.Multiplay.Authoring.Core.MultiplayApi
{
    /// <summary>
    /// Identifying metadata for a long entry that identifies the event's origin.
    /// </summary>
    /// <param name="FleetId">ID of the fleet where the log event originates from.</param>
    /// <param name="MessageId">Unique ID of the message.</param>
    /// <param name="ServerId">ID of the server where the log event originates from.</param>
    /// <param name="Source">Source (most likely a path to a file) where the log event was originally written</param>
    /// <param name="Timestamp">Timestamp of log ingestion event, in ISO 8601 format.</param>
    public record LogEntryMetadata(System.Guid FleetId, string MessageId, string ServerId, string Source, DateTime Timestamp);
    /// <summary>
    /// A single log event returned by the search, including identifying metadata.
    /// </summary>
    /// <param name="Message">The log line message.</param>
    /// <param name="MetaData">Parameter metadata of LogEntry</param>
    public record LogEntry(string Message, LogEntryMetadata MetaData);
    /// <summary>
    /// Log entries found along with their metadata
    /// </summary>
    /// <param name="Count">Number of results returned.</param>
    /// <param name="Entries">Log entries returned by the search.</param>
    /// <param name="PaginationToken">A token used to fetch the next page of results.</param>
    public record LogSearchResult(decimal Count, List<LogEntry> Entries, string PaginationToken);
}
