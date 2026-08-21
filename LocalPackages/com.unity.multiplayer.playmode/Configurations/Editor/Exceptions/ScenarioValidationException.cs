using System;

namespace Unity.Multiplayer.PlayMode.Configurations.Editor.Exceptions
{
    internal class ScenarioValidationException : Exception
    {
        public ScenarioValidationException(string message) : base(message) { }
    }
}
