using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace SampleProjectAV.CustomUpmManager.Editor
{
    public sealed class UnityPackageManagerClient : IUnityPackageManagerClient
    {
        public async Task<string> AddAsync(string packageIdentifier, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(packageIdentifier))
                throw new ArgumentException("Package identifier is required.", nameof(packageIdentifier));

            var request = Client.Add(packageIdentifier);
            await WaitForRequestAsync(request, cancellationToken);
            return request.Result?.name;
        }

        public async Task RemoveAsync(string packageName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(packageName))
                throw new ArgumentException("Package name is required.", nameof(packageName));

            var request = Client.Remove(packageName);
            await WaitForRequestAsync(request, cancellationToken);
        }

        private static async Task WaitForRequestAsync(Request request, CancellationToken cancellationToken)
        {
            while (!request.IsCompleted)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(100, cancellationToken);
            }

            if (request.Status == StatusCode.Failure)
                throw new InvalidOperationException(request.Error?.message ?? "Unity Package Manager request failed.");
        }
    }
}
