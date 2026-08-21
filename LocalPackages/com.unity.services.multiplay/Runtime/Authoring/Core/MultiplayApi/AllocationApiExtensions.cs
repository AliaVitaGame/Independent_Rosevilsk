using System;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Services.Multiplay.Authoring.Core.MultiplayApi
{
    static class AllocationApiExtensions
    {
        public static async Task<AllocationInformation> WaitForAllocation(this IAllocationApi api, FleetId fleetId, Guid allocationId, CancellationToken cancellationToken = default, ITaskDelay delayer = null)
        {
            delayer = delayer ?? new DefaultTaskDelay();
            var retries = 60;
            while (retries-- > 0)
            {
                var result = await api.GetTestAllocation(fleetId, allocationId, cancellationToken);
                if (result == null)
                {
                    await delayer.Delay(5000, cancellationToken);
                }
                else
                {
                    return result;
                }
                if (cancellationToken.IsCancellationRequested)
                    break;
            }
            throw new TimeoutException("The operation was canceled or timed out while waiting for test allocation to be created");
        }
    }
}
