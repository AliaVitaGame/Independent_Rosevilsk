using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Multiplay.Authoring.Core.MultiplayApi;
using Unity.Services.Multiplay.Authoring.Editor.AdminApis.Allocations.Allocations;
using Unity.Services.Multiplay.Authoring.Editor.AdminApis.Allocations.Apis.Allocations;
using Unity.Services.Multiplay.Authoring.Editor.AdminApis.Allocations.Models;

namespace Unity.Services.Multiplay.Authoring.Editor.MultiplayApis
{
    class AllocationApi : IAllocationApi
    {
        readonly IAllocationsApiClient m_Client;
        IMultiplayApiConfig m_ApiConfig;
        readonly IApiAuthenticator m_ApiInit;

        public AllocationApi(IAllocationsApiClient client, IApiAuthenticator apiInit)
        {
            m_Client = client;
            m_ApiInit = apiInit;
            m_ApiConfig = ApiConfig.Empty;
        }

        public async Task InitAsync()
        {
            var(config, basePath, headers) = await m_ApiInit.Authenticate();
            ((AllocationsApiClient)m_Client).Configuration = new AdminApis.Allocations.Configuration(
                basePath,
                null,
                null,
                headers);
            m_ApiConfig = config;
        }

        public async Task<AllocationResult> CreateTestAllocation(FleetId fleetId, Guid regionId, long buildConfigurationId, CancellationToken cancellationToken = default)
        {
            var emptyGuid = Guid.NewGuid();
            var form = new TestAllocateRequestForm(regionId, buildConfigurationId, emptyGuid);

            var request = new ProcessTestAllocationRequest(m_ApiConfig.ProjectId, m_ApiConfig.EnvironmentId, fleetId.Guid, form);
            var response = await TryCatchRequestAsync(request, async(req) => {
                return await m_Client.ProcessTestAllocationAsync(request, null);
            });
            return new AllocationResult(response.Result.AllocationId, response.Result.Href);
        }

        public async Task<AllocationInformation> GetTestAllocation(FleetId fleetId, Guid allocationId,
            CancellationToken cancellationToken = default)
        {
            var request = new GetTestAllocationRequest(m_ApiConfig.ProjectId, m_ApiConfig.EnvironmentId, fleetId.Guid, allocationId);
            var response = await TryCatchRequestAsync(request, async(req) => {
                return await m_Client.GetTestAllocationAsync(request, null);
            });

            if (response.Status != 200 || response.Result.ServerId == 0)
            {
                return null;
            }
            return new AllocationInformation(
                response.Result.AllocationId,
                response.Result.FleetId,
                response.Result.RegionId,
                response.Result.BuildConfigurationId,
                response.Result.ServerId,
                response.Result.MachineId,
                response.Result.Ipv4,
                response.Result.Ipv6,
                response.Result.GamePort);
        }

        public async Task<List<AllocationInformation>> ListTestAllocations(FleetId fleetId, CancellationToken cancellationToken = default)
        {
            var request = new ListTestAllocationsRequest(m_ApiConfig.ProjectId, m_ApiConfig.EnvironmentId);
            var response = await TryCatchRequestAsync(request, async(req) => {
                return await m_Client.ListTestAllocationsAsync(request, null);
            });

            if (response.Status != 200)
            {
                return null;
            }

            var res = new List<AllocationInformation>();
            foreach (var ai in response.Result.Allocations)
            {
                res.Add(
                    new AllocationInformation(
                        ai.AllocationId,
                        ai.FleetId,
                        ai.RegionId,
                        ai.BuildConfigurationId,
                        ai.ServerId,
                        ai.MachineId,
                        ai.Ipv4,
                        ai.Ipv6,
                        ai.GamePort));
            }

            return res;
        }

        public async Task RemoveTestAllocation(FleetId fleetId, Guid allocationId, CancellationToken cancellationToken = default)
        {
            var request = new ProcessTestDeallocationRequest(m_ApiConfig.ProjectId, m_ApiConfig.EnvironmentId, fleetId.Guid, allocationId);
            var response = await TryCatchRequestAsync(request, async(req) => {
                return await m_Client.ProcessTestDeallocationAsync(request, null);
            });
            if (response.Status > 299)
            {
                throw new MultiplayAuthoringException(
                    (int)response.Status,
                    $"Response does not indicate success. Http Status '{response.Status}'");
            }
        }

        async Task<AdminApis.Allocations.Response<TResponse>> TryCatchRequestAsync<TRequest, TResponse>(TRequest request, Func<TRequest, Task<AdminApis.Allocations.Response<TResponse>>> func)
        {
            AdminApis.Allocations.Response<TResponse> response;
            try
            {
                response = await func(request);
            }
            catch (AdminApis.Allocations.Http.HttpException<ListTestAllocations400Response> ex)
            {
                throw new MultiplayAuthoringException((int)ex.Response.StatusCode, ex.ActualError.Detail, ex);
            }
            catch (AdminApis.Allocations.Http.HttpException<ListTestAllocations401Response> ex)
            {
                throw new MultiplayAuthoringException((int)ex.Response.StatusCode, ex.ActualError.Detail, ex);
            }
            catch (AdminApis.Allocations.Http.HttpException<ListTestAllocations403Response> ex)
            {
                throw new MultiplayAuthoringException((int)ex.Response.StatusCode, ex.ActualError.Detail, ex);
            }
            catch (AdminApis.Allocations.Http.HttpException<ListTestAllocations404Response> ex)
            {
                throw new MultiplayAuthoringException((int)ex.Response.StatusCode, ex.ActualError.Detail, ex);
            }
            catch (AdminApis.Allocations.Http.HttpException<ListTestAllocations429Response> ex)
            {
                throw new MultiplayAuthoringException((int)ex.Response.StatusCode, ex.ActualError.Detail, ex);
            }
            catch (AdminApis.Allocations.Http.HttpException<ListTestAllocations500Response> ex)
            {
                throw new MultiplayAuthoringException((int)ex.Response.StatusCode, "Internal Server Error", ex);
            }
            return response;
        }

        async Task<AdminApis.Allocations.Response> TryCatchRequestAsync<TRequest>(TRequest request, Func<TRequest, Task<AdminApis.Allocations.Response>> func)
        {
            AdminApis.Allocations.Response response;
            try
            {
                response = await func(request);
            }
            catch (AdminApis.Allocations.Http.HttpException<ListTestAllocations400Response> ex)
            {
                throw new MultiplayAuthoringException((int)ex.Response.StatusCode, ex.ActualError.Detail, ex);
            }
            catch (AdminApis.Allocations.Http.HttpException<ListTestAllocations401Response> ex)
            {
                throw new MultiplayAuthoringException((int)ex.Response.StatusCode, ex.ActualError.Detail, ex);
            }
            catch (AdminApis.Allocations.Http.HttpException<ListTestAllocations403Response> ex)
            {
                throw new MultiplayAuthoringException((int)ex.Response.StatusCode, ex.ActualError.Detail, ex);
            }
            catch (AdminApis.Allocations.Http.HttpException<ListTestAllocations404Response> ex)
            {
                throw new MultiplayAuthoringException((int)ex.Response.StatusCode, ex.ActualError.Detail, ex);
            }
            catch (AdminApis.Allocations.Http.HttpException<ListTestAllocations429Response> ex)
            {
                throw new MultiplayAuthoringException((int)ex.Response.StatusCode, ex.ActualError.Detail, ex);
            }
            catch (AdminApis.Allocations.Http.HttpException<ListTestAllocations500Response> ex)
            {
                throw new MultiplayAuthoringException((int)ex.Response.StatusCode, "Internal Server Error", ex);
            }
            return response;
        }
    }
}
