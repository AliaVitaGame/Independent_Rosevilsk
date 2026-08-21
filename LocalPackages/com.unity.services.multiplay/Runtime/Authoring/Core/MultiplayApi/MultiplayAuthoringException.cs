using System;

namespace Unity.Services.Multiplay.Authoring.Core.MultiplayApi
{
    class MultiplayAuthoringException : Exception
    {
        int Reason { get; }
        public MultiplayAuthoringException(int code, string message) : base(message)
        {
            Reason = code;
        }

        public MultiplayAuthoringException(int code, string message, Exception innerException) : base(message, innerException)
        {
            Reason = code;
        }
    }
}
