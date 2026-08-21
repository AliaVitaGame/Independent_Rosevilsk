using System;

namespace Unity.Services.Multiplay.Authoring.Editor.MultiplayApis
{
    interface IMultiplayApiConfig
    {
        Guid ProjectId { get; }
        Guid EnvironmentId { get; }
    }
}
