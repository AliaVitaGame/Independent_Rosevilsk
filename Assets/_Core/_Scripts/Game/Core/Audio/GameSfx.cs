using HEAVYART.TopDownShooter.Netcode;
using Game.Core.UI;
using UnityEngine;

namespace Game.Core.Audio
{
    public static class GameSfx
    {
        private const float GunshotMinDistance = 10f;
        private const float GunshotMaxDistance = 48f;
        private const int GunshotPoolSize = 16;

        private static AudioSource _ui;
        private static AudioSource _world;
        private static AudioSource[] _gunshotPool;
        private static int _gunshotPoolIndex;
        private static float _nextHitTime;
        private static float _nextGunTime;
        private static AudioClip _hornClip;
        private static bool _weaponHooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void HookCombatAudio()
        {
            if (_weaponHooked)
                return;

            ExtraCombatTargets.WeaponFired -= PlayGunshot;
            ExtraCombatTargets.WeaponFired += PlayGunshot;
            _weaponHooked = true;
        }

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

        public static void PlayVehicleCrash(float impactSpeed)
        {
            var heavy = impactSpeed >= 8f;
            var clip = heavy
                ? Load("Assets/_Core/_Content/Audios/SFX/Deadly Kombat Free version/metal_punch_finisher_07.wav", "metal_punch_finisher_07")
                  ?? Load("Assets/_Core/_Content/Audios/SFX/Deadly Kombat Free version/body_hit_large_32.wav", "body_hit_large_32")
                : Load("Assets/_Core/_Content/Audios/SFX/Deadly Kombat Free version/metal_punch_06.wav", "metal_punch_06")
                  ?? Load("Assets/_Core/_Content/Audios/SFX/Deadly Kombat Free version/body_hit_small_20.wav", "body_hit_small_20");

            var volume = Mathf.Clamp(0.35f + impactSpeed * 0.04f, 0.35f, 0.9f);
            PlayWorld(clip, volume);
        }

        public static void PlayCarHorn()
        {
            PlayWorld(GetHornClip(), 0.75f);
        }

        public static void PlayGunshot(Vector3 worldPosition)
        {
            if (Time.unscaledTime < _nextGunTime)
                return;

            _nextGunTime = Time.unscaledTime + 0.045f;
            var clip = Load("Assets/_Core/_Content/Audios/SFX/Deadly Kombat Free version/fire_punch_02.wav", "fire_punch_02")
                       ?? Load("Assets/_Core/_Content/Audios/SFX/Deadly Kombat Free version/fire_punch_finisher_06.wav", "fire_punch_finisher_06")
                       ?? Load("Assets/_Core/_Content/Audios/SFX/Deadly Kombat Free version/blade_hit_07.wav", "blade_hit_07");
            PlaySpatial(clip, worldPosition, 0.55f, GunshotMinDistance, GunshotMaxDistance);
        }

        public static void PlayPassengerBoarded()
        {
            PlayWorld(Load("Assets/_Core/_Content/Audios/SFX/Deadly Kombat Free version/punch_short_whoosh_16.wav", "punch_short_whoosh_16"), 0.45f);
        }

        private static AudioClip GetHornClip()
        {
            if (_hornClip != null)
                return _hornClip;

            const int sampleRate = 22050;
            const float duration = 0.42f;
            var samples = Mathf.CeilToInt(sampleRate * duration);
            _hornClip = AudioClip.Create("CarHorn", samples, 1, sampleRate, false);
            var data = new float[samples];
            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)sampleRate;
                var envelope = t < 0.04f
                    ? t / 0.04f
                    : t > duration - 0.08f
                        ? Mathf.Max(0f, (duration - t) / 0.08f)
                        : 1f;
                data[i] = envelope * 0.42f * (
                    Mathf.Sin(2f * Mathf.PI * 415f * t)
                    + 0.45f * Mathf.Sin(2f * Mathf.PI * 830f * t));
            }

            _hornClip.SetData(data, 0);
            return _hornClip;
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

        private static void PlaySpatial(AudioClip clip, Vector3 worldPosition, float volume, float minDistance, float maxDistance)
        {
            if (clip == null)
                return;

            var source = NextGunshotSource();
            source.transform.position = worldPosition;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.PlayOneShot(clip, volume);
        }

        private static AudioSource NextGunshotSource()
        {
            if (_gunshotPool == null)
            {
                var host = new GameObject("GunshotSfx");
                Object.DontDestroyOnLoad(host);
                _gunshotPool = new AudioSource[GunshotPoolSize];
                for (var i = 0; i < GunshotPoolSize; i++)
                {
                    var child = new GameObject($"Gunshot_{i}");
                    child.transform.SetParent(host.transform, false);
                    var source = child.AddComponent<AudioSource>();
                    source.playOnAwake = false;
                    source.spatialBlend = 1f;
                    source.rolloffMode = AudioRolloffMode.Linear;
                    source.dopplerLevel = 0f;
                    source.minDistance = GunshotMinDistance;
                    source.maxDistance = GunshotMaxDistance;
                    _gunshotPool[i] = source;
                }
            }

            var next = _gunshotPool[_gunshotPoolIndex];
            _gunshotPoolIndex = (_gunshotPoolIndex + 1) % _gunshotPool.Length;
            return next;
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
