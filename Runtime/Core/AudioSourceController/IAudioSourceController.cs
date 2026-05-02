using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace CreativeArcana.Audio
{
    public interface IAudioSourceController
    {
        event Action<int,AudioEndReason> OnEnded;
        AudioChannel CurrentChannel { get; }
        void Initialize(Dictionary<AudioChannel, AudioMixerGroup> mixerGroups);

        #region Playback

        bool Play(AudioHandle handle, AudioEntry entry, AudioClip clip, float fadeInDuration = 0);
        void Stop(float fadeOutDuration = 0);

        #endregion

        #region Pause & Resume

        void Pause(float fadeOutDuration = 0);
        void Resume(float fadeInDuration = 0);

        #endregion

        #region Runtime Control

        void SetVolume(float volume);
        void SetPitch(float pitch);
        void SetLoop(bool loop);
        void SetChannel(AudioChannel audioChannel);

        bool IsPlaying();

        #endregion

        #region Spatial Audio

        void Set2D();
        void Set3D(float minDistance = 1f, float maxDistance = 500f);
        void SetPosition(Vector3 position);
        void SetFollowTarget(Transform target);

        #endregion
    }
}