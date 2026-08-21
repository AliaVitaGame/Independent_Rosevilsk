using Unity.Services.Multiplay.Authoring.Editor.Shared.Analytics;

namespace Unity.Services.Multiplay.Authoring.Editor.Analytics
{
    class DummyCcdAnalytics : ICcdAnalytics
    {
        public AnalyticsTimer BeginUpload()
        {
            return new AnalyticsTimer(_ => {});
        }

        public void UploadFailed(string message)
        {
        }
    }
}
