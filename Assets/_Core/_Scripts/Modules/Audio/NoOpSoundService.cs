using System;
using R3;
using Modules.Settings;

namespace Modules.Audio
{
    public sealed class NoOpSoundService : ISoundService
    {
        private readonly IDisposable _subscription;
        private bool _isSoundEnabled = true;

        public NoOpSoundService(ISettingsService settingsService)
        {
            _subscription = settingsService.SoundEnabled.Subscribe(value => _isSoundEnabled = value);
        }

        public void Play(SoundId soundId)
        {
            if (!_isSoundEnabled) return;
        }

        public void Dispose() => _subscription.Dispose();
    }
}
