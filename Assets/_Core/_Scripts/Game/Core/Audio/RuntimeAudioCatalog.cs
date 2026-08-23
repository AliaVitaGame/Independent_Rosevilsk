using UnityEngine;

namespace Game.Core.Audio
{
    [CreateAssetMenu(menuName = "Game/Audio/Runtime Audio Catalog", fileName = "RuntimeAudioCatalog")]
    public sealed class RuntimeAudioCatalog : ScriptableObject
    {
        [SerializeField] private AudioClip[] _clips;

        private static RuntimeAudioCatalog _instance;

        public static AudioClip FindClip(string clipName)
        {
            if (string.IsNullOrEmpty(clipName))
                return null;

            if (_instance == null)
                _instance = Resources.Load<RuntimeAudioCatalog>("RuntimeAudioCatalog");

            if (_instance == null || _instance._clips == null)
                return null;

            for (var i = 0; i < _instance._clips.Length; i++)
            {
                var clip = _instance._clips[i];
                if (clip != null && clip.name == clipName)
                    return clip;
            }

            return null;
        }
    }
}
