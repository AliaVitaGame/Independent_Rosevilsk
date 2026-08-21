using System.Runtime.CompilerServices;

namespace Extensions.Console.Interfaces
{
    public interface IDebugLogger
    {
        void Log(string message, [CallerMemberName] string caller = "");
        void LogWarning(string message, [CallerMemberName] string caller = "");
        void LogError(string message, [CallerMemberName] string caller = "");
    }
}