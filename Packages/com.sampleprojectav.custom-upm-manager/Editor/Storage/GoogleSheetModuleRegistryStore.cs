using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SampleProjectAV.CustomUpmManager.Editor
{
    public sealed class GoogleSheetModuleRegistryStore : IModuleRegistryStore
    {
        public const string DefaultSpreadsheetUrl = "https://docs.google.com/spreadsheets/d/15QVhNhdguL50j_8zzofHACE22VdGe7xF9mjxYvKy8HY/edit?gid=0#gid=0";

        private readonly IModuleRegistryStore localStore;
        private readonly string spreadsheetUrl;

        public GoogleSheetModuleRegistryStore(IModuleRegistryStore localStore, string spreadsheetUrl)
        {
            this.localStore = localStore;
            this.spreadsheetUrl = spreadsheetUrl;
        }

        public CustomUpmRegistry Load()
        {
            try
            {
                var csv = DownloadCsv();
                var registry = GoogleSheetModuleRegistryParser.Parse(csv);
                localStore.Save(registry);
                return registry;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to load Custom UPM modules from Google Sheet. Using local cache. {exception.Message}");
                return localStore.Load();
            }
        }

        public void Save(CustomUpmRegistry registry)
        {
            localStore.Save(registry);
        }

        public void OpenSpreadsheet()
        {
            Application.OpenURL(spreadsheetUrl);
        }

        public static IReadOnlyList<string> BuildCsvExportUrls(string url)
        {
            var idMatch = Regex.Match(url ?? string.Empty, @"/spreadsheets/d/([^/]+)");
            if (!idMatch.Success)
                throw new InvalidOperationException("Google Sheet URL does not contain a spreadsheet id.");

            var gidMatch = Regex.Match(url, @"(?:[?&#]gid=)(\d+)");
            var gid = gidMatch.Success ? gidMatch.Groups[1].Value : "0";
            var id = idMatch.Groups[1].Value;

            // headers=1 is required: without it gviz concatenates every column into a single row.
            return new[]
            {
                $"https://docs.google.com/spreadsheets/d/{id}/gviz/tq?tqx=out:csv&gid={gid}&headers=1",
                $"https://docs.google.com/spreadsheets/d/{id}/export?format=csv&gid={gid}"
            };
        }

        private string DownloadCsv()
        {
            Exception lastException = null;
            foreach (var url in BuildCsvExportUrls(spreadsheetUrl))
            {
                try
                {
                    var csv = DownloadString(url);
                    if (IsHtmlResponse(csv))
                        throw new InvalidOperationException("Google Sheet returned an HTML page instead of CSV.");

                    if (!GoogleSheetModuleRegistryParser.HasRequiredHeaders(csv))
                        throw new InvalidOperationException("Google Sheet CSV is missing required columns.");

                    return csv;
                }
                catch (Exception exception)
                {
                    lastException = exception;
                }
            }

            throw lastException ?? new InvalidOperationException("Failed to download Google Sheet CSV.");
        }

        private static string DownloadString(string url)
        {
            using (var client = new TimeoutWebClient())
            {
                client.Encoding = System.Text.Encoding.UTF8;
                client.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (compatible; CustomUpmManager/1.0)";
                return client.DownloadString(url);
            }
        }

        private static bool IsHtmlResponse(string csv)
        {
            var trimmed = (csv ?? string.Empty).TrimStart();
            return trimmed.StartsWith("<", StringComparison.Ordinal);
        }

        private sealed class TimeoutWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri address)
            {
                var request = base.GetWebRequest(address);
                if (request != null)
                    request.Timeout = 10000;

                return request;
            }
        }
    }
}
