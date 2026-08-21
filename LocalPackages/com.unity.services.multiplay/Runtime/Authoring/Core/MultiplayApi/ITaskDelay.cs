using System;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Services.Multiplay.Authoring.Core.MultiplayApi
{
    interface ITaskDelay
    {
        Task Delay(int millisecondsDelay, CancellationToken cancellationToken = default);
        Task Delay(TimeSpan timeSpan, CancellationToken cancellationToken = default);
    }

    class DefaultTaskDelay : ITaskDelay
    {
        public Task Delay(int millisecondsDelay, CancellationToken cancellationToken = default)
        {
            return Task.Delay(millisecondsDelay, cancellationToken);
        }

        public Task Delay(TimeSpan timeSpan, CancellationToken cancellationToken = default)
        {
            return Task.Delay(timeSpan, cancellationToken);
        }
    }
}
