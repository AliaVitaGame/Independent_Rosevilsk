using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Unity.Services.Multiplay.Authoring.Core.CloudContent
{
    static class BatchExtensions
    {
        public static async Task BatchAsync<T>(this IEnumerable<T> items, int size, Func<T, Task> action)
        {
            foreach (var batch in items.Batch(size))
            {
                await Task.WhenAll(batch.Select(action));
            }
        }

        static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
        {
            var buffer = new List<T>(batchSize);

            foreach (T item in source)
            {
                buffer.Add(item);

                if (buffer.Count >= batchSize)
                {
                    yield return buffer;
                    buffer = new List<T>();
                }
            }
            if (buffer.Any())
            {
                yield return buffer;
            }
        }
    }
}
