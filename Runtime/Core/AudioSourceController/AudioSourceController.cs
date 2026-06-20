using System;
using System.Collections.Generic;
using CreativeArcana.Factory;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;

namespace CreativeArcana.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioSourceController : MonoBehaviour, IAudioSourceController, IPoolable
    {
        public event Action<int, AudioEndReason> OnEnded;

        public AudioChannel CurrentChannel => _audioChannel;

        private AudioSource _audioSource;

        private Dictionary<AudioChannel, AudioMixerGroup> _mixerGroups;

        private Tween _fadeTween;

        private AudioHandle _handle = AudioHandle.Invalid;

        private float _targetVolume = 1f;

        private AudioChannel _audioChannel;

        private Transform _followTarget;

        private bool _isInitialized;

        private bool _wasPaused;

        private bool _hasReportedEnded;

        private bool _hasStartedPlaying;

        private int _playFrame;

        private double _expectedEndDspTime;

        private bool _hasExpectedEndTime;

        private void Update()
        {
            if (_hasReportedEnded)
                return;

            if (IsAudioFinished())
                NotifyFinished();
        }

        private void LateUpdate()
        {
            if (_followTarget != null)
                transform.position = _followTarget.position;
        }

        public void Initialize(Dictionary<AudioChannel, AudioMixerGroup> mixerGroups)
        {
            _mixerGroups = mixerGroups ?? throw new ArgumentNullException(nameof(mixerGroups));

            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
                _audioSource.playOnAwake = false;
            }

            _isInitialized = true;
        }

        public bool Play(AudioHandle handle, AudioEntry entry, AudioClip clip, float fadeInDuration = 0)
        {
            ThrowExceptionIfNotInitialized();

            if (entry == null || clip == null)
                return false;

            KillFade();

            ResetPlaybackStateForPlay(handle);

            if (!SetAudioMixerGroup(entry.AudioChannel))
                return false;

            _targetVolume = Mathf.Max(0f, entry.GetVolume());

            _audioSource.clip = clip;
            _audioSource.pitch = entry.GetPitch();
            _audioSource.loop = entry.Loop;
            _audioSource.volume = fadeInDuration > 0f ? 0f : _targetVolume;

            ApplySpatialSettings(entry);

            _audioSource.Play();

            RegisterExpectedEndTimeFromCurrentState();

            if (fadeInDuration > 0f)
                _fadeTween = FadeVolume(_targetVolume, fadeInDuration);

            return true;
        }

        public void Stop(float fadeOutDuration = 0)
        {
            if (_audioSource == null)
                return;

            KillFade();
            ClearExpectedEndTime();

            if (fadeOutDuration <= 0f)
            {
                _audioSource.Stop();
                NotifyStopped();
                return;
            }

            _fadeTween = FadeVolume(0f, fadeOutDuration)
                .OnComplete(() =>
                {
                    if (_audioSource != null)
                        _audioSource.Stop();

                    NotifyStopped();
                });
        }

        public void Pause(float fadeOutDuration = 0)
        {
            if (_audioSource == null)
                return;

            KillFade();

            if (!_audioSource.isPlaying)
                return;

            _wasPaused = true;
            ClearExpectedEndTime();

            if (fadeOutDuration <= 0f)
            {
                _audioSource.Pause();
                return;
            }

            _fadeTween = FadeVolume(0f, fadeOutDuration)
                .OnComplete(() =>
                {
                    if (_audioSource == null)
                        return;

                    _audioSource.Pause();
                    _audioSource.volume = _targetVolume;
                });
        }

        public void Resume(float fadeInDuration = 0)
        {
            if (_audioSource == null || _audioSource.clip == null)
                return;

            if (!_wasPaused)
                return;

            KillFade();

            _wasPaused = false;

            if (fadeInDuration <= 0f)
            {
                _audioSource.volume = _targetVolume;
                _audioSource.UnPause();
                RegisterExpectedEndTimeFromCurrentState();
                return;
            }

            _audioSource.volume = 0f;
            _audioSource.UnPause();

            RegisterExpectedEndTimeFromCurrentState();

            _fadeTween = FadeVolume(_targetVolume, fadeInDuration);
        }

        public void SetVolume(float volume)
        {
            if (_audioSource == null)
                return;

            _targetVolume = Mathf.Max(0f, volume);

            KillFade();
            _audioSource.volume = _targetVolume;
        }

        public void SetPitch(float pitch)
        {
            if (_audioSource == null)
                return;

            _audioSource.pitch = pitch;

            if (_audioSource.clip != null && !_audioSource.loop && !_wasPaused)
                RegisterExpectedEndTimeFromCurrentState();
        }

        public void SetLoop(bool loop)
        {
            if (_audioSource == null)
                return;

            _audioSource.loop = loop;

            if (loop)
                ClearExpectedEndTime();
            else
                RegisterExpectedEndTimeFromCurrentState();
        }

        public void SetChannel(AudioChannel audioChannel)
        {
            SetAudioMixerGroup(audioChannel);
        }

        public bool IsPlaying()
        {
            return _audioSource != null && _audioSource.isPlaying;
        }

        public void Set2D()
        {
            if (_audioSource == null)
                return;

            _audioSource.spatialBlend = 0f;
            _audioSource.dopplerLevel = 0f;
            _audioSource.spread = 0;
        }

        public void Set3D(float minDistance = 1, float maxDistance = 500)
        {
            if (_audioSource == null)
                return;

            _audioSource.spatialBlend = 1f;
            _audioSource.dopplerLevel = 1f;
            _audioSource.spread = 0;
            _audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            _audioSource.minDistance = Mathf.Max(0.01f, minDistance);
            _audioSource.maxDistance = Mathf.Max(_audioSource.minDistance, maxDistance);
        }

        public void SetPosition(Vector3 position)
        {
            transform.position = position;
        }

        public void SetFollowTarget(Transform target)
        {
            _followTarget = target;
        }

        public void OnPoolCreate()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;
        }

        public void OnPoolGet()
        {
        }

        public void OnPoolRelease()
        {
            ResetState();
        }

        public void OnPoolDestroy()
        {
            ResetState();
        }

        private void ResetPlaybackStateForPlay(AudioHandle handle)
        {
            _handle = handle;

            _followTarget = null;

            _wasPaused = false;
            _hasReportedEnded = false;
            _hasStartedPlaying = false;

            _playFrame = Time.frameCount;

            ClearExpectedEndTime();
        }

        private bool IsAudioFinished()
        {
            if (_audioSource == null || _audioSource.clip == null)
                return false;

            if (_audioSource.loop || _wasPaused)
                return false;

            // Avoid false "finished" report on the same frame as Play().
            if (Time.frameCount == _playFrame)
                return false;

            if (_audioSource.isPlaying)
            {
                _hasStartedPlaying = true;
                return false;
            }

            if (_hasStartedPlaying)
                return true;

            // Very short clips can start and finish between two Update calls.
            return _hasExpectedEndTime && AudioSettings.dspTime >= _expectedEndDspTime;
        }

        private void RegisterExpectedEndTimeFromCurrentState()
        {
            ClearExpectedEndTime();

            if (_audioSource == null || _audioSource.clip == null)
                return;

            if (_audioSource.loop || _wasPaused)
                return;

            var pitch = Mathf.Abs(_audioSource.pitch);

            if (pitch <= 0.0001f)
                return;

            var remainingDuration = GetRemainingClipDuration() / pitch;

            // Small grace prevents reporting finished before Unity updates AudioSource state.
            _expectedEndDspTime = AudioSettings.dspTime + remainingDuration + 0.02d;
            _hasExpectedEndTime = true;
        }

        private float GetRemainingClipDuration()
        {
            if (_audioSource == null || _audioSource.clip == null)
                return 0f;

            var clipLength = _audioSource.clip.length;
            var currentTime = Mathf.Clamp(_audioSource.time, 0f, clipLength);

            return Mathf.Max(0f, clipLength - currentTime);
        }

        private void ClearExpectedEndTime()
        {
            _expectedEndDspTime = 0d;
            _hasExpectedEndTime = false;
        }

        private void ApplySpatialSettings(AudioEntry entry)
        {
            if (_audioSource == null)
                return;

            if (entry.Spatial)
            {
                _audioSource.spatialBlend = entry.SpatialBlend;
                _audioSource.dopplerLevel = entry.DopplerLevel;
                _audioSource.spread = entry.Spread;
                _audioSource.rolloffMode = entry.RolloffMode;
                _audioSource.minDistance = entry.MinDistance;
                _audioSource.maxDistance = entry.MaxDistance;
            }
            else
            {
                Set2D();
            }
        }

        private void ResetState()
        {
            KillFade();
            ClearExpectedEndTime();

            if (_audioSource != null)
            {
                _audioSource.Stop();
                _audioSource.clip = null;
                _audioSource.volume = 1f;
                _audioSource.pitch = 1f;
                _audioSource.loop = false;

                _audioSource.spatialBlend = 0f;
                _audioSource.dopplerLevel = 0f;
                _audioSource.spread = 0;
                _audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
                _audioSource.minDistance = 1f;
                _audioSource.maxDistance = 500f;

                _audioSource.outputAudioMixerGroup = null;
            }

            _handle = AudioHandle.Invalid;
            _targetVolume = 1f;

            _audioChannel = default;
            _followTarget = null;

            _wasPaused = false;
            _hasReportedEnded = false;
            _hasStartedPlaying = false;
            _playFrame = 0;

            OnEnded = null;

            _mixerGroups = null;
            _isInitialized = false;
        }

        private void KillFade()
        {
            if (_fadeTween == null)
                return;

            if (_fadeTween.IsActive())
                _fadeTween.Kill();

            _fadeTween = null;
        }

        private Tween FadeVolume(float targetVolume, float duration)
        {
            return DOTween.To(
                    () => _audioSource != null ? _audioSource.volume : 0f,
                    value =>
                    {
                        if (_audioSource != null)
                            _audioSource.volume = value;
                    },
                    targetVolume,
                    duration
                )
                .SetEase(Ease.Linear)
                .SetTarget(this);
        }

        private bool SetAudioMixerGroup(AudioChannel audioChannel)
        {
            if (_audioSource == null)
                return false;

            if (_mixerGroups == null)
            {
                Debug.LogError("[AudioSourceController] Mixer groups are not initialized.", this);
                return false;
            }

            if (!_mixerGroups.TryGetValue(audioChannel, out var mixerGroup))
            {
                Debug.LogError($"[AudioSourceController] Mixer group not found for channel: {audioChannel}", this);
                return false;
            }

            _audioChannel = audioChannel;
            _audioSource.outputAudioMixerGroup = mixerGroup;

            return true;
        }

        private void ThrowExceptionIfNotInitialized()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException(
                    "[AudioSourceController] Call Initialize() before using this method.");
            }
        }

        private void NotifyFinished()
        {
            if (_hasReportedEnded)
                return;

            _hasReportedEnded = true;
            ClearExpectedEndTime();

            OnEnded?.Invoke(_handle.Id, AudioEndReason.Finished);
        }

        private void NotifyStopped()
        {
            if (_hasReportedEnded)
                return;

            _hasReportedEnded = true;
            ClearExpectedEndTime();

            OnEnded?.Invoke(_handle.Id, AudioEndReason.Stopped);
        }
    }
}
