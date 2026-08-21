using System;

namespace Modules.Audio
{
    public interface ISoundService : IDisposable
    {
        void Play(SoundId soundId);
    }
}
