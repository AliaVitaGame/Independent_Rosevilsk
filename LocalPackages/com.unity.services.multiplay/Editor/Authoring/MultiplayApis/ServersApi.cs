using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Multiplay.Authoring.Core.MultiplayApi;
using Unity.Services.Multiplay.Authoring.Editor.AdminApis.Servers.Servers;
using Unity.Services.Multiplay.Authoring.Editor.AdminApis.Servers.Apis.Servers;
using Unity.Services.Multiplay.Authoring.Editor.AdminApis.Servers.Models;
using ActionLog = Unity.Services.Multiplay.Authoring.Core.MultiplayApi.ActionLog;

namespace Unity.Services.Multiplay.Authoring.Editor.MultiplayApis
{
    class ServersApi : IServersApi
    {
        readonly IServersApiClient m_Client;
        readonly IApiAuthenticator m_ApiInit;
        IMultiplayApiConfig m_ApiConfig;

        public ServersApi(IServersApiClient client, IApiAuthenticator apiInit)
        {
            m_Client = client;
            m_ApiInit = apiInit;
            m_ApiConfig = ApiConfig.Empty;
        }

        public async Task InitAsync()
        {
            var(config, basePath, headers) = await m_ApiInit.Authenticate();
            ((ServersApiClient)m_Client).Configuration = new AdminApis.Servers.Configuration(
                basePath,
                null,
                null,
                headers);
            m_ApiConfig = config;
        }

        public async Task<bool> TriggerServerActionAsync(long serverId, ServerAction action, CancellationToken cancellationToken = default)
        {
            var actionRequest = new ActionRequest((ActionRequest.ActionOptions)action);
            var serverAction = new TriggerServerActionRequest(
                m_ApiConfig.ProjectId,
                m_ApiConfig.EnvironmentId,
                serverId,
                actionRequest);

            var response = await TryCatchRequestAsync(actionRequest, async(req) => {
                return await m_Client.TriggerServerActionAsync(serverAction);
            });
            return response.Result.Success;
        }

        public async Task<ServerInfo> GetServerAsync(long serverId, CancellationToken cancellationToken = default)
        {
            var serverRequest = new GetServerRequest(
                m_ApiConfig.ProjectId,
                m_ApiConfig.EnvironmentId,
                serverId);

            var response = await TryCatchRequestAsync(serverRequest, async(req) => {
                return await m_Client.GetServerAsync(serverRequest);
            });

            return new ServerInfo(
                response.Result.Id,
                response.Result.MachineID,
                response.Result.MachineName,
                response.Result.BuildConfigurationID,
                response.Result.BuildConfigurationName,
                response.Result.BuildName,
                response.Result.FleetID,
                response.Result.FleetName,
                response.Result.LocationID,
                response.Result.LocationName,
                response.Result.Ip,
                response.Result.Port,
                (ServerStatus)response.Result.Status,
                response.Result.CpuLimit,
                response.Result.MemoryLimit,
                response.Result.Deleted,
                response.Result.HoldExpiresAt);
        }

        public async Task<List<ActionLog>> GetServerActionLogsAsync(long serverId, CancellationToken cancellationToken = default)
        {
            var serverRequest = new GetServerActionsRequest(
                m_ApiConfig.ProjectId,
                m_ApiConfig.EnvironmentId,
                serverId);

            var response = await TryCatchRequestAsync(serverRequest, async(req) => {
                return await m_Client.GetServerActionsAsync(serverRequest);
            });

            return response.Result.Select(a => new ActionLog(
                a.Id,
                a.ServerID,
                a.ActionID,
                a.MachineID,
                a.Message,
                a.Date,
                a.Attachment)).ToList();
        }

        async Task<AdminApis.Servers.Response<TResponse>> TryCatchRequestAsync<TRequest, TResponse>(TRequest request, Func<TRequest, Task<AdminApis.Servers.Response<TResponse>>> func)
        {
            AdminApis.Servers.Response<TResponse> response;
            try
            {
                response = await func(request);
            }
            catch (AdminApis.Servers.Http.HttpException<InlineResponse400> ex)
            {
                throw new MultiplayAuthoringException((int)ex.Response.StatusCode, ex.ActualError.Detail, ex);
            }
            catch (AdminApis.Servers.Http.HttpException<InlineResponse401> ex)
            {
                throw new MultiplayAuthoringException((int)ex.Response.StatusCode, ex.ActualError.Detail, ex);
            }
            catch (AdminApis.Servers.Http.HttpException<InlineResponse403> ex)
            {
                throw new MultiplayAuthoringException((int)ex.Response.StatusCode, ex.ActualError.Detail, ex);
            }
            catch (AdminApis.Servers.Http.HttpException<InlineResponse404> ex)
            {
                throw new MultiplayAuthoringException((int)ex.Response.StatusCode, ex.ActualError.Detail, ex);
            }
            catch (AdminApis.Servers.Http.HttpException<InlineResponse424> ex)
            {
                throw new MultiplayAuthoringException((int)ex.Response.StatusCode, ex.ActualError.Detail, ex);
            }
            catch (AdminApis.Servers.Http.HttpException<InlineResponse429> ex)
            {
                throw new MultiplayAuthoringException((int)ex.Response.StatusCode, ex.ActualError.Detail, ex);
            }
            catch (AdminApis.Servers.Http.HttpException<InlineResponse500> ex)
            {
                throw new MultiplayAuthoringException((int)ex.Response.StatusCode, "Internal Server Error", ex);
            }
            return response;
        }
    }
}
