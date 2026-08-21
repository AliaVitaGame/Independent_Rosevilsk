using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SampleProjectAV.CustomUpmManager.Editor
{
    public sealed class DownloadClient : IDownloadClient
    {
        public async Task DownloadAsync(string url, string destinationPath, IProgress<float> progress, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("Download URL is required.", nameof(url));

            if (string.IsNullOrWhiteSpace(destinationPath))
                throw new ArgumentException("Destination path is required.", nameof(destinationPath));

            var destinationDirectory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(destinationDirectory))
                Directory.CreateDirectory(destinationDirectory);

            var temporaryPath = destinationPath + ".part";
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);

            using (var client = new HttpClient())
            using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                response.EnsureSuccessStatusCode();
                var totalBytes = response.Content.Headers.ContentLength;
                var downloadedBytes = 0L;
                var buffer = new byte[81920];

                using (var source = await response.Content.ReadAsStreamAsync())
                using (var destination = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None, buffer.Length, true))
                {
                    while (true)
                    {
                        var read = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                        if (read == 0)
                            break;

                        await destination.WriteAsync(buffer, 0, read, cancellationToken);
                        downloadedBytes += read;

                        if (totalBytes.HasValue && totalBytes.Value > 0)
                            progress?.Report((float)downloadedBytes / totalBytes.Value);
                    }
                }
            }

            if (File.Exists(destinationPath))
                File.Delete(destinationPath);

            File.Move(temporaryPath, destinationPath);
            progress?.Report(1f);
        }
    }
}
