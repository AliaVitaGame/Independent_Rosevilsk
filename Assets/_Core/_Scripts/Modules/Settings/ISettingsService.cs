using R3;

namespace Modules.Settings
{
    public interface ISettingsService
    {
        ReadOnlyReactiveProperty<bool> SoundEnabled { get; }
        ReadOnlyReactiveProperty<bool> MusicEnabled { get; }
        ReadOnlyReactiveProperty<bool> HapticsEnabled { get; }
        void SetSoundEnabled(bool value);
        void SetMusicEnabled(bool value);
        void SetHapticsEnabled(bool value);
    }
}
