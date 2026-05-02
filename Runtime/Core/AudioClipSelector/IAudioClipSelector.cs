using UnityEngine;

namespace CreativeArcana.Audio
{
    public interface IAudioClipSelector
    {
        AudioClip GetClip(AudioEntry entry);
        void Clear();
    }
}