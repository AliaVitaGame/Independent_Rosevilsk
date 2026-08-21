using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace SampleProjectAV.CustomUpmManager.Editor
{
    public sealed class DocumentationPreview
    {
        public DocumentationPreview(string title, string description, string imageUrl, byte[] imageData)
        {
            Title = title;
            Description = description;
            ImageUrl = imageUrl;
            ImageData = imageData;
        }

        public string Title { get; }
        public string Description { get; }
        public string ImageUrl { get; }
        public byte[] ImageData { get; }
    }

    public static class DocumentationPreviewClient
    {
        private static readonly Dictionary<string, Task<DocumentationPreview>> PreviewTasks = new Dictionary<string, Task<DocumentationPreview>>();

        public static Task<DocumentationPreview> GetPreviewAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return Task.FromResult(new DocumentationPreview("Documentation", string.Empty, string.Empty, null));

            lock (PreviewTasks)
            {
                if (!PreviewTasks.TryGetValue(url, out var task))
                {
                    task = LoadPreviewAsync(url);
                    PreviewTasks[url] = task;
                }

                return task;
            }
        }

        public static async Task PrefetchAsync(IEnumerable<string> urls)
        {
            var distinctUrls = urls
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            using (var gate = new SemaphoreSlim(4))
            {
                var tasks = distinctUrls.Select(url => PrefetchOneAsync(url, gate));
                await Task.WhenAll(tasks);
            }
        }

        private static async Task<DocumentationPreview> LoadPreviewAsync(string url)
        {
            var html = await GetTextAsync(url);
            var title = FirstNonEmpty(GetMetaValue(html, "og:title"), GetMetaValue(html, "twitter:title"), GetTitle(html), url);
            var description = FirstNonEmpty(GetMetaValue(html, "og:description"), GetMetaValue(html, "twitter:description"));
            var imageUrl = ResolveUrl(url, FirstNonEmpty(GetMetaValue(html, "og:image"), GetMetaValue(html, "twitter:image")));
            var imageData = await TryLoadImageAsync(imageUrl);
            return new DocumentationPreview(title, description, imageUrl, imageData);
        }

        private static async Task PrefetchOneAsync(string url, SemaphoreSlim gate)
        {
            await gate.WaitAsync();
            try
            {
                await GetPreviewAsync(url);
            }
            catch
            {
                // A preview is optional; failed links must not affect the manager window.
            }
            finally
            {
                gate.Release();
            }
        }

        private static async Task<byte[]> TryLoadImageAsync(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return null;

            try
            {
                var imageData = await GetBytesAsync(imageUrl);
                return imageData.Length <= 5 * 1024 * 1024 ? imageData : null;
            }
            catch
            {
                return null;
            }
        }

        private static async Task<string> GetTextAsync(string url)
        {
            return await SendRequestAsync(url, request => request.downloadHandler.text);
        }

        private static async Task<byte[]> GetBytesAsync(string url)
        {
            return await SendRequestAsync(url, request => request.downloadHandler.data);
        }

        private static Task<T> SendRequestAsync<T>(string url, System.Func<UnityWebRequest, T> getResult)
        {
            var completion = new TaskCompletionSource<T>();
            var request = UnityWebRequest.Get(url);
            request.timeout = 10;
            request.SetRequestHeader("User-Agent", "TelegramBot (like TwitterBot)");
            var operation = request.SendWebRequest();
            operation.completed += _ =>
            {
                try
                {
                    if (request.result != UnityWebRequest.Result.Success)
                        completion.TrySetException(new System.InvalidOperationException($"Request failed ({request.responseCode}): {request.error}"));
                    else
                        completion.TrySetResult(getResult(request));
                }
                finally
                {
                    request.Dispose();
                }
            };

            return completion.Task;
        }

        private static string GetMetaValue(string html, string name)
        {
            foreach (Match match in Regex.Matches(html ?? string.Empty, "<meta\\b[^>]*>", RegexOptions.IgnoreCase))
            {
                var tag = match.Value;
                if (!string.Equals(GetAttribute(tag, "property"), name, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(GetAttribute(tag, "name"), name, StringComparison.OrdinalIgnoreCase))
                    continue;

                return WebUtility.HtmlDecode(GetAttribute(tag, "content") ?? string.Empty).Trim();
            }

            return string.Empty;
        }

        private static string GetTitle(string html)
        {
            var match = Regex.Match(html ?? string.Empty, "<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value).Trim() : string.Empty;
        }

        private static string GetAttribute(string tag, string name)
        {
            var match = Regex.Match(tag, $"\\b{name}\\s*=\\s*['\"]([^'\"]*)['\"]", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private static string ResolveUrl(string pageUrl, string candidate)
        {
            if (string.IsNullOrWhiteSpace(candidate))
                return string.Empty;

            return Uri.TryCreate(new Uri(pageUrl), candidate, out var result) ? result.AbsoluteUri : candidate;
        }

        private static string FirstNonEmpty(params string[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return string.Empty;
        }

    }
}
