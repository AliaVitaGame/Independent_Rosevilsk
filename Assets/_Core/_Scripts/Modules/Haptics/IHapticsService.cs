using System;

namespace Modules.Haptics
{
    public interface IHapticsService : IDisposable
    {
        void Vibrate(HapticType hapticType);
    }
}
