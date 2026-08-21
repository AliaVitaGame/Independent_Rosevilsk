// WARNING: Auto generated code. Modifications will be lost!
using System;

namespace Unity.Services.Multiplay.Authoring.Editor.Shared.Chrono
{
    interface ICurrentTime
    {
        DateTime Now { get; }
    }

    class CurrentTime : ICurrentTime
    {
        public DateTime Now => DateTime.Now;
    }
}
