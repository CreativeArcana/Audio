using UnityEngine;

namespace CreativeArcana.Audio
{
    /// <summary>
    /// Dependency: AudioClip - Unity Audio System
    /// Not Suitable for FMOD or other audio systems
    /// </summary>
    public interface IAudioService
    {
        #region Playback

        AudioHandle Play(AudioEntry entry, float fadeInDuration = 0, DuckingProfile duckingProfile = default);
        AudioHandle Play(AudioId audioId, float fadeInDuration = 0, DuckingProfile duckingProfile = default);

        void Stop(AudioHandle handle, float fadeOutDuration = 0);
        void StopAll(float fadeOutDuration = 0);
        void StopAll(AudioChannel channel, float fadeOutDuration = 0);

        AudioHandle CrossFadePlay(AudioHandle from, AudioEntry to, float fadeInDuration = 1, float fadeOutDuration = 1,
            DuckingProfile duckingProfile = default);

        AudioHandle CrossFadePlay(AudioHandle from, AudioId to, float fadeInDuration = 1, float fadeOutDuration = 1,
            DuckingProfile duckingProfile = default);

        #endregion

        #region Pause & Resume

        void Pause(AudioHandle handle, float fadeOutDuration = 0);
        void PauseAll(float fadeOutDuration = 0);
        void PauseAll(AudioChannel channel, float fadeOutDuration = 0);

        void Resume(AudioHandle handle, float fadeInDuration = 0);
        void ResumeAll(float fadeInDuration = 0);
        void ResumeAll(AudioChannel channel, float fadeInDuration = 0);

        #endregion

        #region Runtime Control

        void SetVolume(AudioHandle handle, float volume);
        void SetPitch(AudioHandle handle, float pitch);
        void SetLoop(AudioHandle handle, bool loop);
        void SetChannel(AudioHandle handle, AudioChannel channel);

        bool IsPlaying(AudioHandle handle);

        #endregion

        #region Channels & Mixing

        void SetChannelBaseVolume(AudioChannel channel, float volume);
        void SetChannelVolumeMultiplier(AudioChannel channel, float multiplier, float fadeDuration = 0);
        float GetChannelEffectiveVolume(AudioChannel channel);

        void MuteChannel(AudioChannel channel, bool mute);
        bool IsChannelMuted(AudioChannel channel);

        #endregion

        #region Spatial Audio

        void Set2D(AudioHandle handle);
        void Set3D(AudioHandle handle, float minDistance = 1f, float maxDistance = 500f);
        void SetPosition(AudioHandle handle, Vector3 position);
        void SetFollowTarget(AudioHandle handle, Transform target);

        #endregion

        #region Save/Load

        void SaveChannelVolume(AudioChannel channel);
        float LoadChannelVolume(AudioChannel channel);

        #endregion

        #region Audio Lifetime

        bool SetLifetime(AudioHandle handle, AudioLifetime lifetime);
        void StopSceneBoundAudio(float fadeOutDuration = 0f);
        void SetAutoStopSceneBoundAudio(bool isOn);

        #endregion

        //TODO: #region Audio States & Snapshots
        //
        // void SetAudioState(AudioState state);
        //
        // AudioState GetCurrentAudioState();
        //
        // void ApplySnapshot(
        //     AudioSnapshot snapshot,
        //     float transitionTime = 0.3f
        // );
        //
        // #endregion

        //TODO: #region Addressables / Streaming
        //
        // /// <summary>
        // /// Preloads audio data (Addressables / Streaming).
        // /// </summary>
        // void Preload(AudioId audioId);
        //
        // void Release(AudioId audioId);
        //
        // #endregion

        //TODO: #region Debug & Diagnostics or subtitles sync or gameplay reaction
        //
        // int ActiveVoiceCount { get; }
        //
        // event Action<AudioHandle> OnAudioStarted;
        // event Action<AudioHandle> OnAudioStopped;
        //
        // #endregion
    }
}