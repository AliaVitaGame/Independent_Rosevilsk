using Unity.Services.Multiplay.Authoring.Editor.Shared.Analytics;

namespace Unity.Services.Multiplay.Authoring.Editor.Analytics
{
    interface ICcdAnalytics
    {
        AnalyticsTimer BeginUpload();
        void UploadFailed(string message);
    }
}
