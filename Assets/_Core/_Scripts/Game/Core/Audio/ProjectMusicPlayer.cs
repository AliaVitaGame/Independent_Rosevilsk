using Game.Core.UI;
using PrimeTween;
using UnityEngine;

namespace Game.Core.Audio
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class ProjectMusicPlayer : MonoBehaviour
    {
        [SerializeField] private AudioClip _clip;
        [SerializeField] [Range(0f, 1f)] private float _volume = 0.55f;
        [SerializeField] private float _fadeDuration = 0.7f;
        [SerializeField] private bool _spatial;
        [SerializeField] private float _minDistance = 18f;
        [SerializeField] private float _maxDistance = 55f;

        public float TargetVolume => _volume;

        public void ConfigureAsVehicleRadio()
        {
            _spatial = true;
            EnsureSource();
        }

        private AudioSource _source;
        private Tween _fadeTween;
        private bool _shouldPlay;

        public void SetShouldPlay(bool play)
        {
            EnsureSource();
            _shouldPlay = play;
            if (play)
                FadeTo(_volume);
            else
                FadeTo(0f);
        }

        private void Awake()
        {
            EnsureSource();
        }

        private void OnDestroy()
        {
            _fadeTween.Stop();
        }

        private void EnsureSource()
        {
            if (_source == null)
                _source = GetComponent<AudioSource>();

            if (_clip == null)
                _clip = GameplayUiStyle.LoadProjectMusic();

            _source.clip = _clip;
            _source.loop = true;
            _source.playOnAwake = false;
            _source.dopplerLevel = 0f;
            _source.spatialBlend = _spatial ? 1f : 0f;
            if (_spatial)
            {
                _source.rolloffMode = AudioRolloffMode.Linear;
                _source.minDistance = _minDistance;
                _source.maxDistance = _maxDistance;
            }
            if (_clip != null && !_source.isPlaying)
            {
                _source.volume = 0f;
                _source.Play();
            }
        }

        private void FadeTo(float volume)
        {
            EnsureSource();
            if (_clip == null)
                return;

            if (!_source.isPlaying)
                _source.Play();

            _fadeTween.Stop();
            _fadeTween = Tween.Custom(
                this,
                startValue: _source.volume,
                endValue: volume,
                duration: _fadeDuration,
                ease: Ease.InOutSine,
                onValueChange: (player, value) => player._source.volume = value);

            if (volume <= 0.001f)
            {
                _fadeTween.OnComplete(() =>
                {
                    if (!_shouldPlay)
                        _source.Pause();
                });
            }
        }
    }
}
