using UnityEngine.Audio;

namespace CreativeArcana.Audio
{
    public interface IAudioMixerGroupController
    {
        AudioMixerGroup MixerGroup { get; }
        bool IsMuted { get; }
        void SetBaseVolume(float volume);
        void SetVolumeMultiplier(float multiplier, float fadeDuration = 0);
        void SetDuckingMultiplier(float multiplier, float fadeDuration = 0);
        void SaveVolume();
        float LoadVolume();
        float GetEffectiveVolume();
        void MuteChannel(bool mute);
    }
}