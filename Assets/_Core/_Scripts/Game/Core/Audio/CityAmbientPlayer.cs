using Game.Core.UI;
using UnityEngine;

namespace Game.Core.Audio
{
    [DisallowMultipleComponent]
    public sealed class CityAmbientPlayer : MonoBehaviour
    {
        private const float QuieterThanMusicFactor = 0.7f;

        [SerializeField] private AudioClip _cityA;
        [SerializeField] private AudioClip _cityB;
        [SerializeField] private float _musicReferenceVolume = 0.55f;

        public static CityAmbientPlayer Ensure()
        {
            var existing = FindFirstObjectByType<CityAmbientPlayer>(FindObjectsInactive.Include);
            if (existing != null)
                return existing;

            var host = new GameObject("CityAmbient");
            return host.AddComponent<CityAmbientPlayer>();
        }

        private void Awake()
        {
            var music = FindFirstObjectByType<ProjectMusicPlayer>(FindObjectsInactive.Include);
            if (music != null)
                _musicReferenceVolume = music.TargetVolume;

            _cityA ??= GameplayUiStyle.LoadClip("Assets/_Core/_Content/Audios/Ambiend/City.mp3", "City");
            _cityB ??= GameplayUiStyle.LoadClip("Assets/_Core/_Content/Audios/Ambiend/City 2.mp3", "City 2");

            var volume = _musicReferenceVolume * QuieterThanMusicFactor;
            CreateLoopSource("CityA", _cityA, volume);
            CreateLoopSource("CityB", _cityB, volume);
        }

        private void CreateLoopSource(string name, AudioClip clip, float volume)
        {
            if (clip == null)
                return;

            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = volume;
            source.Play();
        }
    }
}
