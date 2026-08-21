using System;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor.Nodes.Multiplay.Exceptions
{
    internal class MultiplayEnvironmentException : InvalidOperationException
    {
        public MultiplayEnvironmentException(string message) : base(message) { }
    }
}
