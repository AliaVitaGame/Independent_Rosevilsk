using Unity.Services.Multiplay.Authoring.Core.Logging;

namespace Unity.Services.Multiplay.Authoring.Editor.Logging
{
    class Logger : ILogger
    {
        public void LogInfo(object message)
        {
            Shared.Logging.Logger.Log(message);
        }

        public void LogWarning(object message)
        {
            Shared.Logging.Logger.LogWarning(message);
        }

        public void LogError(object message)
        {
            Shared.Logging.Logger.LogError(message);
        }

        public void LogVerbose(object message)
        {
            Shared.Logging.Logger.LogVerbose(message);
        }
    }
}
