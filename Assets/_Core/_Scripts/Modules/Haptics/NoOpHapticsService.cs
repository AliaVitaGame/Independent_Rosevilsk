using System;
using R3;
using Modules.Settings;

namespace Modules.Haptics
{
    public sealed class NoOpHapticsService : IHapticsService
    {
        private readonly IDisposable _subscription;
        private bool _isHapticsEnabled = true;

        public NoOpHapticsService(ISettingsService settingsService)
        {
            _subscription = settingsService.HapticsEnabled.Subscribe(value => _isHapticsEnabled = value);
        }

        public void Vibrate(HapticType hapticType)
        {
            if (!_isHapticsEnabled) return;
        }

        public void Dispose() => _subscription.Dispose();
    }
}
