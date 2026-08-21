using System;

namespace Unity.Services.Multiplay.Authoring.Editor.MultiplayApis
{
    class DuplicateResourceException : Exception
    {
        public DuplicateResourceException(string resource, string name)
            : base($"found duplicate {resource} of name {name}")
        {
        }
    }
}
