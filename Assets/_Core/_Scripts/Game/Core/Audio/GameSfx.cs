using Game.Core.UI;
using UnityEngine;

namespace Game.Core.Audio
{
    public static class GameSfx
    {
        private static AudioSource _ui;
        private static AudioSource _world;
        private static float _nextHitTime;

        public static void PlayHover()
        {
            PlayUi(Load("Assets/_Core/_Content/Arts/UI/Bloodlines UI/Audio/Hover Button SFX.wav", "Hover Button SFX"), 0.55f);
        }

        public static void PlayClick()
        {
            PlayUi(Load("Assets/_Core/_Content/Arts/UI/Bloodlines UI/Audio/Click Button SFX.wav", "Click Button SFX"), 0.7f);
        }

        public static void PlayCancel()
        {
            PlayUi(Load("Assets/CharacterCustomizer/UI/Audio/UI_Sound_Cancel.wav", "UI_Sound_Cancel"), 0.65f);
        }

        public static void PlayConfirm()
        {
            PlayUi(Load("Assets/CharacterCustomizer/UI/Audio/UI_Sound_Select.wav", "UI_Sound_Select"), 0.7f);
        }

        public static void PlayEnterVehicle()
        {
            PlayWorld(Load("Assets/_Core/_Content/Audios/SFX/Deadly Kombat Free version/punch_short_whoosh_30.wav", "punch_short_whoosh_30"), 0.55f);
        }

        public static void PlayExitVehicle()
        {
            PlayWorld(Load("Assets/_Core/_Content/Audios/SFX/Deadly Kombat Free version/kick_short_whoosh_23.wav", "kick_short_whoosh_23"), 0.5f);
        }

        public static void PlayVehicleHit()
        {
            if (Time.unscaledTime < _nextHitTime)
                return;

            _nextHitTime = Time.unscaledTime + 0.12f;
            PlayWorld(Load("Assets/_Core/_Content/Audios/SFX/Deadly Kombat Free version/metal_punch_06.wav", "metal_punch_06"), 0.6f);
        }

        public static void PlayPassengerBoarded()
        {
            PlayWorld(Load("Assets/_Core/_Content/Audios/SFX/Deadly Kombat Free version/punch_short_whoosh_16.wav", "punch_short_whoosh_16"), 0.45f);
        }

        private static void PlayUi(AudioClip clip, float volume)
        {
            if (clip == null)
                return;

            EnsureUi();
            _ui.PlayOneShot(clip, volume);
        }

        private static void PlayWorld(AudioClip clip, float volume)
        {
            if (clip == null)
                return;

            EnsureWorld();
            _world.PlayOneShot(clip, volume);
        }

        private static void EnsureUi()
        {
            if (_ui != null)
                return;

            var go = new GameObject("UiSfx");
            Object.DontDestroyOnLoad(go);
            _ui = go.AddComponent<AudioSource>();
            _ui.playOnAwake = false;
            _ui.spatialBlend = 0f;
        }

        private static void EnsureWorld()
        {
            if (_world != null)
                return;

            var go = new GameObject("WorldSfx");
            Object.DontDestroyOnLoad(go);
            _world = go.AddComponent<AudioSource>();
            _world.playOnAwake = false;
            _world.spatialBlend = 0f;
        }

        private static AudioClip Load(string path, string name)
        {
            return GameplayUiStyle.LoadClip(path, name);
        }
    }
}
