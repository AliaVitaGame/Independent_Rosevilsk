namespace Game.Meta.Analytics
{
    public interface IAnalyticsService
    {
        void Track(AnalyticsEventType eventType, string payload = null);
    }
}
