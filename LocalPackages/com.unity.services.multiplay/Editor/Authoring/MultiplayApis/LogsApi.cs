using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Multiplay.Authoring.Core.MultiplayApi;
using Unity.Services.Multiplay.Authoring.Editor.AdminApis.Logs.Apis.Logs;
using Unity.Services.Multiplay.Authoring.Editor.AdminApis.Logs.Models;
using Unity.Services.Multiplay.Authoring.Editor.AdminApis.Logs.Logs;
using WebSocketSharp;
using LogEntry = Unity.Services.Multiplay.Authoring.Core.MultiplayApi.LogEntry;
using LogEntryMetadata = Unity.Services.Multiplay.Authoring.Core.MultiplayApi.LogEntryMetadata;

namespace Unity.Services.Multiplay.Authoring.Editor.MultiplayApis
{
    class LogsApi : ILogsApi
    {
        readonly ILogsApiClient m_Client;
        readonly IApiAuthenticator m_ApiInit;
        IMultiplayApiConfig m_ApiConfig;

        public LogsApi(ILogsApiClient client, IApiAuthenticator apiInit)
        {
            m_Client = client;
            m_ApiInit = apiInit;
            m_ApiConfig = ApiConfig.Empty;
        }

        public async Task InitAsync()
        {
            var(config, basePath, headers) = await m_ApiInit.Authenticate();
            ((LogsApiClient)m_Client).Configuration = new AdminApis.Logs.Configuration(
                basePath,
                null,
                null,
                headers);
            m_ApiConfig = config;
        }

        public async Task<LogSearchResult> SearchLogsAsync(LogSearchParams searchParams, CancellationToken cancellationToken = default)
        {
            var paginationToken = searchParams.PaginationToken.IsNullOrEmpty() ? "" : searchParams.PaginationToken;

            var searchLogRequest = new LogSearchRequest(
                searchParams.Query,
                searchParams.Size,
                new LogSearchRequestFilters(
                    searchParams.FleetId,
                    searchParams.ServerId.ToString(),
                    "",
                    searchParams.From,
                    searchParams.To),
                paginationToken);

            var searchRequest = new SearchLogsRequest(
                m_ApiConfig.ProjectId,
                m_ApiConfig.EnvironmentId,
                searchLogRequest);

            var response = await TryCatchRequestAsync(searchRequest, async(req) => {
                return await m_Client.SearchLogsAsync(searchRequest);
            });

            var result = new LogSearchResult(response.Result.Count,
                response.Result.Hits.Select(log => new LogEntry(log.Message, new LogEntryMetadata(
                    log.Metadata.FleetId, log.Metadata.MessageId, log.Metadata.ServerId, log.Metadata.Source, log.Metadata.Timestamp))).ToList(), response.Result.PaginationToken);

            return result;
        }

        async Task<AdminApis.Logs.Response<TResponse>> TryCatchRequestAsync<TRequest, TResponse>(TRequest request, Func<TRequest, Task<AdminApis.Logs.Response<TResponse>>> func)
        {
            AdminApis.Logs.Response<TResponse> response;
            try
            {
                response = await func(request);
            }
            catch (AdminApis.Logs.Http.HttpException<SearchLogs400Response> ex)
            {
                throw new MultiplayAuthoringException((int)ex.Response.StatusCode, ex.ActualError.Detail, ex);
            }
            catch (AdminApis.Logs.Http.HttpException<SearchLogs401Response> ex)
            {
                throw new MultiplayAuthoringException((int)ex.Response.StatusCode, ex.ActualError.Detail, ex);
            }
            catch (AdminApis.Logs.Http.HttpException<SearchLogs403Response> ex)
            {
                throw new MultiplayAuthoringException((int)ex.Response.StatusCode, ex.ActualError.Detail, ex);
            }
            catch (AdminApis.Logs.Http.HttpException<SearchLogs404Response> ex)
            {
                throw new MultiplayAuthoringException((int)ex.Response.StatusCode, ex.ActualError.Detail, ex);
            }
            catch (AdminApis.Logs.Http.HttpException<SearchLogs429Response> ex)
            {
                throw new MultiplayAuthoringException((int)ex.Response.StatusCode, ex.ActualError.Detail, ex);
            }
            catch (AdminApis.Logs.Http.HttpException<SearchLogs500Response> ex)
            {
                throw new MultiplayAuthoringException((int)ex.Response.StatusCode, "Internal Server Error", ex);
            }
            return response;
        }
    }
}
