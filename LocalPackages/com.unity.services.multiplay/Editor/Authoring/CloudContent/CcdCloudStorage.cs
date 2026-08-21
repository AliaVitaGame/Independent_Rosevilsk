using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Multiplay.Authoring.Core.Builds;
using Unity.Services.Multiplay.Authoring.Editor.Analytics;
using Unity.Services.Multiplay.Authoring.Editor.AdminApis.Ccd;
using Unity.Services.Multiplay.Authoring.Editor.AdminApis.Ccd.Apis.Buckets;
using Unity.Services.Multiplay.Authoring.Editor.AdminApis.Ccd.Apis.Entries;
using Unity.Services.Multiplay.Authoring.Editor.AdminApis.Ccd.Buckets;
using Unity.Services.Multiplay.Authoring.Editor.AdminApis.Ccd.Entries;
using Unity.Services.Multiplay.Authoring.Editor.AdminApis.Ccd.Http;
using Unity.Services.Multiplay.Authoring.Editor.AdminApis.Ccd.Models;
using Unity.Services.Multiplay.Authoring.Core.CloudContent;
using Unity.Services.Multiplay.Authoring.Editor.MultiplayApis;
using UnityEngine;
using HttpClient = System.Net.Http.HttpClient;
using ILogger = Unity.Services.Multiplay.Authoring.Core.Logging.ILogger;

namespace Unity.Services.Multiplay.Authoring.Editor.CloudContent
{
    class CcdCloudStorage : ICloudStorage
    {
        const int k_BatchSize = 10;

        IMultiplayApiConfig m_ApiConfig;
        readonly IBucketsApiClient m_BucketsApiClient;
        readonly IEntriesApiClient m_EntriesApiClient;
        readonly HttpClient m_UploadClient;
        readonly ICcdAnalytics m_Analytics;
        readonly IApiAuthenticator m_ApiInit;
        private readonly ILogger m_Logger;

        public CcdCloudStorage(
            IBucketsApiClient bucketsApiClient,
            IEntriesApiClient entriesApiClient,
            HttpClient uploadClient,
            ICcdAnalytics analytics,
            IApiAuthenticator apiInit,
            ILogger logger)
        {
            m_BucketsApiClient = bucketsApiClient;
            m_EntriesApiClient = entriesApiClient;
            m_UploadClient = uploadClient;
            m_Analytics = analytics;
            m_ApiInit = apiInit;
            m_Logger = logger;
            m_ApiConfig = ApiConfig.Empty;
        }

        public async Task InitAsync()
        {
            var(config, basePath, headers) = await m_ApiInit.Authenticate();
            m_ApiConfig = config;
            ((BucketsApiClient)m_BucketsApiClient).Configuration = new AdminApis.Ccd.Configuration(
                basePath,
                null,
                null,
                headers);
            ((EntriesApiClient)m_EntriesApiClient).Configuration = new AdminApis.Ccd.Configuration(
                basePath,
                null,
                null,
                headers);
        }

        public async Task<CloudBucketId> FindBucket(string name)
        {
            var request = new ListBucketsByProjectEnvRequest(m_ApiConfig.EnvironmentId.ToString(), m_ApiConfig.ProjectId.ToString(), name: name);
            var buckets = await m_BucketsApiClient.ListBucketsByProjectEnvAsync(request);
            return buckets.Result.Select(b => new CloudBucketId { Guid = b.Id }).FirstOrDefault();
        }

        public async Task<CloudBucketId> CreateBucket(string name)
        {
            var projectGuid = Guid.Parse(m_ApiConfig.ProjectId.ToString());
            var request = new CreateBucketByProjectEnvRequest(m_ApiConfig.EnvironmentId.ToString(), m_ApiConfig.ProjectId.ToString(), new InlineObject32(name, projectGuid));
            var res = await m_BucketsApiClient.CreateBucketByProjectEnvAsync(request);
            return new CloudBucketId { Guid = res.Result.Id };
        }

        public async Task<int> UploadBuildEntries(CloudBucketId bucket, IList<BuildEntry> localEntries, Action<BuildEntry> onUpdated = null, CancellationToken cancellationToken = default)
        {
            var changes = 0;
            var unchanged = 0;
            var deleted = 0;
            try
            {
                using var upload = m_Analytics.BeginUpload();

                var remoteEntries = await ListAllRemoteEntries(bucket);
                var orphans = remoteEntries.Keys.ToHashSet();

                await localEntries.BatchAsync(k_BatchSize, async local =>
                {
                    var(path, content) = local;
                    var normalizedPath = Normalize(path);
                    var hash = content.CcdHash();
                    var entryExists = remoteEntries.TryGetValue(normalizedPath, out var remoteEntry);
                    if (entryExists)
                    {
                        orphans.Remove(normalizedPath);
                        bool uploadedCorrectly = remoteEntry.Complete;
                        bool hasChanged = remoteEntry.ContentHash.ToLowerInvariant() != hash;
                        if (!uploadedCorrectly)
                        {
                            m_Logger.LogVerbose($"Re-uploading Entry {remoteEntry.Entryid}|{remoteEntry.Path}");
                        }

                        bool shouldSkip = !hasChanged && uploadedCorrectly;
                        if (shouldSkip)
                        {
                            //Dont reupload entry unchanged entry.
                            Interlocked.Increment(ref unchanged);
                            onUpdated?.Invoke(local);
                            return;
                        }
                    }

                    var entry = await CreateOrUpdateEntry(bucket, normalizedPath, hash, (int)content.Length);
                    Interlocked.Increment(ref changes);
                    await UploadSignedContent(entry, content);
                    onUpdated?.Invoke(local);
                });

                await orphans.BatchAsync(k_BatchSize, async orphan =>
                {
                    var entry = remoteEntries[orphan];
                    Interlocked.Increment(ref changes);
                    Interlocked.Increment(ref deleted);
                    await DeleteEntry(bucket, entry);
                });
            }
            catch (Exception e)
            {
                m_Logger.LogVerbose($"Upload failed. Total changes: {changes}; unchanged: {unchanged}; deleted: {deleted}");
                m_Analytics.UploadFailed(e.GetType().ToString());
                throw;
            }

            m_Logger.LogVerbose($"Upload succeed. Total changes: {changes}; unchanged: {unchanged}; deleted: {deleted}");
            return changes;
        }

        public async Task Clear()
        {
            var buckets = await m_BucketsApiClient.ListBucketsByProjectEnvAsync(new ListBucketsByProjectEnvRequest(
                m_ApiConfig.EnvironmentId.ToString(),
                m_ApiConfig.ProjectId.ToString()));

            foreach (var bucket in buckets.Result)
            {
                try
                {
                    await m_BucketsApiClient.DeleteBucketEnvAsync(new DeleteBucketEnvRequest(
                        m_ApiConfig.EnvironmentId.ToString(),
                        bucket.Id.ToString(),
                        m_ApiConfig.ProjectId.ToString()
                    ));
                }
                catch (HttpException e) when (e.Response.StatusCode == 403)
                {
                    // Ignore Http 403 errors until there is a better way to determine permissions
                    // https://unity.slack.com/archives/CLYNN0XC7/p1660586331118719
                    // Tracked with https://jira.unity3d.com/browse/CCS-2740
                }
            }
        }

        async Task DeleteEntry(CloudBucketId bucket, InlineResponse2003 entry)
        {
            var deleteReq = new DeleteEntryEnvRequest(
                m_ApiConfig.EnvironmentId.ToString(),
                bucket.ToString(),
                entry.Entryid.ToString(),
                m_ApiConfig.ProjectId.ToString());
            await m_EntriesApiClient.DeleteEntryEnvAsync(deleteReq);
        }

        async Task<InlineResponse2003> CreateOrUpdateEntry(CloudBucketId bucket, string path, string hash, int length)
        {
            var create = new InlineObject23(hash, length, signedUrl: true);
            var request = new CreateOrUpdateEntryByPathEnvRequest(
                m_ApiConfig.EnvironmentId.ToString(),
                bucket.ToString(),
                path,
                m_ApiConfig.ProjectId.ToString(),
                create,
                updateIfExists: true);
            var res = await m_EntriesApiClient.CreateOrUpdateEntryByPathEnvAsync(request);
            return res.Result;
        }

        async Task UploadSignedContent(InlineResponse2003 entry, Stream content)
        {
            // Signed uploads need to be done using HTTP Client
            // Unity generated client does not support sending application/offset+octet-stream
            // Timeout and retry is hard to handle here
            var streamContent = new StreamContent(content);
            streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/offset+octet-stream");
            var res = await m_UploadClient.PutAsync(entry.SignedUrl, streamContent);
            res.EnsureSuccessStatusCode();
        }

        async Task<IDictionary<string, InlineResponse2003>> ListAllRemoteEntries(CloudBucketId bucket)
        {
            const int entriesPerPage = 100;
            var entries = new Dictionary<string, InlineResponse2003>();

            Response<List<InlineResponse2003>> res;
            var page = 1;
            do
            {
                var request = new GetEntriesEnvRequest(
                    m_ApiConfig.EnvironmentId.ToString(),
                    bucket.ToString(),
                    m_ApiConfig.ProjectId.ToString(),
                    page,
                    perPage: entriesPerPage);
                try
                {
                    res = await m_EntriesApiClient.GetEntriesEnvAsync(request);
                }
                catch (HttpException exp) when (exp.Response.StatusCode ==
                                                (int)HttpStatusCode.RequestedRangeNotSatisfiable)
                {
                    //page ends exactly at a multiple of 100.
                    break;
                }

                foreach (var entry in res.Result)
                {
                    entries.Add(entry.Path, entry);
                }
                page++;
            }
            while (res.Result.Count == entriesPerPage);

            return entries;
        }

        static string Normalize(string path)
        {
            return path.TrimStart('/');
        }
    }
}
