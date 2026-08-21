namespace Game.Meta.Analytics
{
    public sealed class NoOpAnalyticsService : IAnalyticsService
    {
        public void Track(AnalyticsEventType eventType, string payload = null)
        {
            // TODO: Forward events to the selected analytics SDK.
        }
    }
}
