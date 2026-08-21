using Unity.Services.Multiplay.Authoring.Core.Builds;
using Unity.Services.Multiplay.Authoring.Core.Model;
using Unity.Services.Multiplay.Authoring.Core.MultiplayApi;

namespace Unity.Services.Multiplay.Authoring.Core.Deployment
{
    /// <summary>
    /// Represents the result of a Build upload
    /// </summary>
    /// <param name="BuildItem">The Build Item uploaded</param>
    /// <param name="BuildId">The new Build ID associated with the build uploaded</param>
    /// <param name="CloudBucketId">The cloud bucket where the direct file as uploaded to</param>
    /// <param name="Changes">Number of changes introduced by the upload</param>
    public record BuildUploadResult(
        BuildItem BuildItem,
        BuildId BuildId,
        CloudBucketId CloudBucketId,
        int Changes);
}
