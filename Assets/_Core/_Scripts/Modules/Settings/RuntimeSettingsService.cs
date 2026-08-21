using R3;

namespace Modules.Settings
{
    public sealed class RuntimeSettingsService : ISettingsService
    {
        public ReadOnlyReactiveProperty<bool> SoundEnabled => _soundEnabled;
        public ReadOnlyReactiveProperty<bool> MusicEnabled => _musicEnabled;
        public ReadOnlyReactiveProperty<bool> HapticsEnabled => _hapticsEnabled;

        private readonly ReactiveProperty<bool> _soundEnabled = new(true);
        private readonly ReactiveProperty<bool> _musicEnabled = new(true);
        private readonly ReactiveProperty<bool> _hapticsEnabled = new(true);

        public void SetSoundEnabled(bool value) => _soundEnabled.Value = value;
        public void SetMusicEnabled(bool value) => _musicEnabled.Value = value;
        public void SetHapticsEnabled(bool value) => _hapticsEnabled.Value = value;

        // TODO: Persist these values through the future durable save service.
    }
}
