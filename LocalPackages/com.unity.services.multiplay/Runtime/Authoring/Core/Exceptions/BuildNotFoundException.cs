using System;

namespace Unity.Services.Multiplay.Authoring.Core.Exceptions
{
    class BuildNotFoundException : Exception
    {
        public BuildNotFoundException(string buildName)
            : base($"The Build '{buildName}' could not be found") {}
    }
}
