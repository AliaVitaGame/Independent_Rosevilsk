using System;

namespace Unity.Services.Multiplay.Authoring.Core.Exceptions
{
    class BuildFailedException : Exception
    {
        public BuildFailedException(string message, Exception inner = null)
            : base(message, inner) {}
    }
}
