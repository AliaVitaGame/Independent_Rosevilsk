namespace Unity.Services.Multiplay.Authoring.Core.Logging
{
    interface ILogger
    {
        void LogInfo(object message);
        void LogWarning(object message);
        void LogError(object message);
        void LogVerbose(object message);
    }
}
