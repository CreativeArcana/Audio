using System;
using System.Collections.Generic;
using System.Linq;
using CreativeArcana.Factory;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace CreativeArcana.Audio
{
    public class AudioService : IAudioService, IDisposable
    {
        private readonly AudioLibrary _library;
        private readonly AudioMixer _audioMixer;
        private readonly IPoolFactory<IAudioSourceController> _sourceControllerFactory;

        // int = AudioHandle.Id
        private readonly Dictionary<int, ActiveAudio> _activeAudios = new();

        private readonly Dictionary<AudioChannel, IAudioMixerGroupController> _mixerGroupControllers = new();
        private readonly Dictionary<AudioChannel, AudioMixerGroup> _mixerGroups = new();

        private IDuckingManager _duckingManager;
        private IAudioClipSelector _clipSelector;

        private int _nextHandleId;
        private bool _autoStopSceneBoundAudio = true;
        private bool _disposed;

        public AudioService(
            AudioMixer audioMixer,
            AudioLibrary library,
            IPoolFactory<IAudioSourceController> sourceControllerFactory)
        {
            _audioMixer = audioMixer ?? throw new ArgumentNullException(nameof(audioMixer));
            _library = library ?? throw new ArgumentNullException(nameof(library));
            _sourceControllerFactory = sourceControllerFactory ??
                                       throw new ArgumentNullException(nameof(sourceControllerFactory));

            Initialize();
        }

        public AudioHandle Play(AudioEntry entry, float fadeInDuration = 0, DuckingProfile duckingProfile = default)
        {
            ThrowIfDisposed();

            if (entry == null)
            {
                Debug.LogWarning("[AudioService] Play failed: entry is null.");
                return AudioHandle.Invalid;
            }

            var clip = _clipSelector.GetClip(entry);
            if (clip == null)
            {
                Debug.LogWarning($"[AudioService] Play failed: no valid clip found for entry '{entry.name}'.");
                return AudioHandle.Invalid;
            }

            var sourceController = _sourceControllerFactory.Get();

            if (sourceController == null)
            {
                Debug.LogError("[AudioService] Play failed: source controller factory returned null.");
                return AudioHandle.Invalid;
            }

            sourceController.Initialize(_mixerGroups);
            sourceController.OnEnded += OnEnded;

            var handle = CreateHandle();

            // Register before Play, because very short clips may end immediately.
            _activeAudios.Add(handle.Id, new ActiveAudio(sourceController, entry.Lifetime));
            _duckingManager.Apply(handle.Id, duckingProfile);

            if (!sourceController.Play(handle, entry, clip, fadeInDuration))
            {
                if (_activeAudios.Remove(handle.Id, out var activeAudio))
                {
                    activeAudio.SourceController.OnEnded -= OnEnded;
                    _duckingManager.Release(handle.Id);
                    _sourceControllerFactory.Release(activeAudio.SourceController);
                }

                return AudioHandle.Invalid;
            }

            return handle;
        }

        public AudioHandle Play(AudioId audioId, float fadeInDuration = 0, DuckingProfile duckingProfile = default)
        {
            ThrowIfDisposed();

            var entry = _library.Get(audioId);

            if (entry == null)
                return AudioHandle.Invalid;

            return Play(entry, fadeInDuration, duckingProfile);
        }

        public void Stop(AudioHandle handle, float fadeOutDuration = 0)
        {
            if (!TryGetSourceController(handle, out var sourceController))
                return;

            sourceController.Stop(fadeOutDuration);

            // Ducking release is intentionally handled in OnEnded.
            // For fade-out, ducking remains active until the source actually stops.
        }

        public void StopAll(float fadeOutDuration = 0)
        {
            if (_disposed)
                return;

            var activeAudios = _activeAudios.Values.ToArray();

            foreach (var activeAudio in activeAudios)
            {
                activeAudio.SourceController.Stop(fadeOutDuration);
            }
        }

        public void StopAll(AudioChannel channel, float fadeOutDuration = 0)
        {
            if (_disposed)
                return;

            var activeAudios = _activeAudios.Values
                .Where(x => x.SourceController.CurrentChannel == channel)
                .ToArray();

            foreach (var activeAudio in activeAudios)
            {
                activeAudio.SourceController.Stop(fadeOutDuration);
            }
        }

        public AudioHandle CrossFadePlay(
            AudioHandle from,
            AudioEntry to,
            float fadeInDuration = 1,
            float fadeOutDuration = 1,
            DuckingProfile duckingProfile = default)
        {
            var newHandle = Play(to, fadeInDuration, duckingProfile);
            if (!newHandle.IsValid)
                return AudioHandle.Invalid;

            Stop(from, fadeOutDuration);
            return newHandle;
        }

        public AudioHandle CrossFadePlay(
            AudioHandle from,
            AudioId to,
            float fadeInDuration = 1,
            float fadeOutDuration = 1,
            DuckingProfile duckingProfile = default)
        {
            var newHandle = Play(to, fadeInDuration, duckingProfile);
            if (!newHandle.IsValid)
                return AudioHandle.Invalid;

            Stop(from, fadeOutDuration);
            return newHandle;
        }

        public void Pause(AudioHandle handle, float fadeOutDuration = 0)
        {
            if (!TryGetSourceController(handle, out var sourceController))
                return;

            sourceController.Pause(fadeOutDuration);

            // Important:
            // While this source is paused, its ducking should not affect other channels.
            _duckingManager.Pause(handle.Id);
        }

        public void PauseAll(float fadeOutDuration = 0)
        {
            if (_disposed)
                return;

            var activeAudiosArray = _activeAudios.ToArray();

            foreach (var kvp in activeAudiosArray)
            {
                var id = kvp.Key;
                var activeAudio = kvp.Value;

                activeAudio.SourceController.Pause(fadeOutDuration);
                _duckingManager.Pause(id);
            }
        }

        public void PauseAll(AudioChannel channel, float fadeOutDuration = 0)
        {
            if (_disposed)
                return;

            var activeAudiosArray = _activeAudios
                .Where(x => x.Value.SourceController.CurrentChannel == channel)
                .ToArray();

            foreach (var kvp in activeAudiosArray)
            {
                var id = kvp.Key;
                var activeAudio = kvp.Value;

                activeAudio.SourceController.Pause(fadeOutDuration);
                _duckingManager.Pause(id);
            }
        }

        public void Resume(AudioHandle handle, float fadeInDuration = 0)
        {
            if (!TryGetSourceController(handle, out var sourceController))
                return;

            sourceController.Resume(fadeInDuration);

            // Re-apply ducking as the newest active ducking source.
            _duckingManager.Resume(handle.Id);
        }

        public void ResumeAll(float fadeInDuration = 0)
        {
            if (_disposed)
                return;

            var activeAudiosArray = _activeAudios.ToArray();

            foreach (var kvp in activeAudiosArray)
            {
                var id = kvp.Key;
                var activeAudio = kvp.Value;

                activeAudio.SourceController.Resume(fadeInDuration);
                _duckingManager.Resume(id);
            }
        }

        public void ResumeAll(AudioChannel channel, float fadeInDuration = 0)
        {
            if (_disposed)
                return;

            var activeAudiosArray = _activeAudios
                .Where(x => x.Value.SourceController.CurrentChannel == channel)
                .ToArray();

            foreach (var kvp in activeAudiosArray)
            {
                var id = kvp.Key;
                var activeAudio = kvp.Value;

                activeAudio.SourceController.Resume(fadeInDuration);
                _duckingManager.Resume(id);
            }
        }

        public void SetVolume(AudioHandle handle, float volume)
        {
            if (!TryGetSourceController(handle, out var sourceController))
                return;

            sourceController.SetVolume(volume);
        }

        public void SetPitch(AudioHandle handle, float pitch)
        {
            if (!TryGetSourceController(handle, out var sourceController))
                return;

            sourceController.SetPitch(pitch);
        }

        public void SetLoop(AudioHandle handle, bool loop)
        {
            if (!TryGetSourceController(handle, out var sourceController))
                return;

            sourceController.SetLoop(loop);
        }

        public void SetChannel(AudioHandle handle, AudioChannel channel)
        {
            if (!TryGetSourceController(handle, out var sourceController))
                return;

            sourceController.SetChannel(channel);
        }

        public bool IsPlaying(AudioHandle handle)
        {
            if (!TryGetSourceController(handle, out var sourceController))
                return false;

            return sourceController.IsPlaying();
        }

        public void SetChannelVolumeMultiplier(AudioChannel channel, float multiplier, float fadeDuration = 0)
        {
            if (!TryGetMixerGroupController(channel, out var mixer))
                return;

            multiplier = Mathf.Clamp01(multiplier);
            mixer.SetVolumeMultiplier(multiplier, fadeDuration);
        }

        public float GetChannelEffectiveVolume(AudioChannel channel)
        {
            if (!TryGetMixerGroupController(channel, out var mixer))
                return 0f;

            return mixer.GetEffectiveVolume();
        }

        public void MuteChannel(AudioChannel channel, bool mute)
        {
            if (!TryGetMixerGroupController(channel, out var mixer))
                return;

            mixer.MuteChannel(mute);
        }

        public bool IsChannelMuted(AudioChannel channel)
        {
            if (!TryGetMixerGroupController(channel, out var mixer))
                return false;

            return mixer.IsMuted;
        }

        public void Set2D(AudioHandle handle)
        {
            if (!TryGetSourceController(handle, out var sourceController))
                return;

            sourceController.Set2D();
        }

        public void Set3D(AudioHandle handle, float minDistance = 1, float maxDistance = 500)
        {
            if (!TryGetSourceController(handle, out var sourceController))
                return;

            sourceController.Set3D(minDistance, maxDistance);
        }

        public void SetPosition(AudioHandle handle, Vector3 position)
        {
            if (!TryGetSourceController(handle, out var sourceController))
                return;

            sourceController.SetPosition(position);
        }

        public void SetFollowTarget(AudioHandle handle, Transform target)
        {
            if (!TryGetSourceController(handle, out var sourceController))
                return;

            sourceController.SetFollowTarget(target);
        }

        public void SetChannelBaseVolume(AudioChannel channel, float volume)
        {
            if (!TryGetMixerGroupController(channel, out var mixer))
                return;

            mixer.SetBaseVolume(volume);
        }

        public void SaveChannelVolume(AudioChannel channel)
        {
            if (!TryGetMixerGroupController(channel, out var mixer))
                return;

            mixer.SaveVolume();
        }

        public float LoadChannelVolume(AudioChannel channel)
        {
            if (!TryGetMixerGroupController(channel, out var mixer))
                return 1f;

            return mixer.LoadVolume();
        }

        public bool SetLifetime(AudioHandle handle, AudioLifetime lifetime)
        {
            if (_disposed)
                return false;

            if (!handle.IsValid)
                return false;

            if (!_activeAudios.TryGetValue(handle.Id, out var activeAudio))
                return false;

            activeAudio.SetLifetime(lifetime);
            return true;
        }

        public void StopSceneBoundAudio(float fadeOutDuration = 0)
        {
            var activeAudiosArray = _activeAudios.ToArray();

            foreach (var activeAudio in activeAudiosArray)
            {
                if (activeAudio.Value.Lifetime == AudioLifetime.SceneBound)
                {
                    Stop(new AudioHandle(activeAudio.Key), fadeOutDuration);
                }
            }
        }

        public void SetAutoStopSceneBoundAudio(bool isOn)
        {
            _autoStopSceneBoundAudio = isOn;
        }

        private void Initialize()
        {
            _sourceControllerFactory.ApplyInitialPreWarm();

            InitializeAudioMixerGroupControllers();
            InitializeAudioMixerGroups();
            LoadAndSetAudioMixerVolumes();

            _duckingManager = new DuckingManager(SetChannelDuckingMultiplier);
            _clipSelector = new AudioClipSelector();

            SceneManager.sceneLoaded += OnSceneLoad;
        }

        private void LoadAndSetAudioMixerVolumes()
        {
            foreach (var mixerGroup in _mixerGroupControllers.Values)
            {
                var loadedVolume = mixerGroup.LoadVolume();
                mixerGroup.SetBaseVolume(loadedVolume);
            }
        }

        private void InitializeAudioMixerGroupControllers()
        {
            _mixerGroupControllers[AudioChannel.Master] =
                new AudioMixerGroupController(
                    _audioMixer,
                    AudioMixerGroupNames.MASTER,
                    AudioMixerParameterNames.MASTER_VOLUME);

            _mixerGroupControllers[AudioChannel.Music] =
                new AudioMixerGroupController(
                    _audioMixer,
                    AudioMixerGroupNames.MUSIC,
                    AudioMixerParameterNames.MUSIC_VOLUME);

            _mixerGroupControllers[AudioChannel.SFX] =
                new AudioMixerGroupController(
                    _audioMixer,
                    AudioMixerGroupNames.SFX,
                    AudioMixerParameterNames.SFX_VOLUME);

            _mixerGroupControllers[AudioChannel.UI] =
                new AudioMixerGroupController(
                    _audioMixer,
                    AudioMixerGroupNames.UI,
                    AudioMixerParameterNames.UI_VOLUME);

            _mixerGroupControllers[AudioChannel.Voice] =
                new AudioMixerGroupController(
                    _audioMixer,
                    AudioMixerGroupNames.VOICE,
                    AudioMixerParameterNames.VOICE_VOLUME);

            _mixerGroupControllers[AudioChannel.Ambient] =
                new AudioMixerGroupController(
                    _audioMixer,
                    AudioMixerGroupNames.AMBIENT,
                    AudioMixerParameterNames.AMBIENT_VOLUME);
        }

        private void InitializeAudioMixerGroups()
        {
            foreach (var kvp in _mixerGroupControllers)
            {
                if (kvp.Value?.MixerGroup == null)
                {
                    Debug.LogError($"[AudioService] MixerGroup is null for channel: {kvp.Key}");
                    continue;
                }

                _mixerGroups[kvp.Key] = kvp.Value.MixerGroup;
            }
        }

        private AudioHandle CreateHandle()
        {
            do
            {
                _nextHandleId++;

                if (_nextHandleId <= 0)
                    _nextHandleId = 1;
            } while (_activeAudios.ContainsKey(_nextHandleId));

            return new AudioHandle(_nextHandleId);
        }


        private void SetChannelDuckingMultiplier(AudioChannel channel, float multiplier, float fadeDuration = 0)
        {
            if (!TryGetMixerGroupController(channel, out var mixer))
                return;

            multiplier = Mathf.Clamp01(multiplier);
            mixer.SetDuckingMultiplier(multiplier, fadeDuration);
        }

        private void OnEnded(int id, AudioEndReason reason)
        {
            if (!_activeAudios.Remove(id, out var activeAudio))
                return;

            activeAudio.SourceController.OnEnded -= OnEnded;

            _duckingManager.Release(id);
            _sourceControllerFactory.Release(activeAudio.SourceController);
        }

        private bool TryGetSourceController(AudioHandle handle, out IAudioSourceController sourceController)
        {
            sourceController = null;

            if (_disposed)
                return false;

            if (!handle.IsValid)
                return false;

            if (!_activeAudios.TryGetValue(handle.Id, out var activeAudio))
                return false;


            sourceController = activeAudio.SourceController;
            return true;
        }

        private bool TryGetMixerGroupController(
            AudioChannel channel,
            out IAudioMixerGroupController mixerGroupController)
        {
            mixerGroupController = null;

            if (_disposed)
                return false;

            if (_mixerGroupControllers.TryGetValue(channel, out mixerGroupController))
                return true;

            Debug.LogError($"[AudioService] Mixer group controller not found for channel: {channel}");
            return false;
        }

        private void OnSceneLoad(Scene scene, LoadSceneMode sceneMode)
        {
            if (_autoStopSceneBoundAudio)
            {
                StopSceneBoundAudio();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AudioService));
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            foreach (var kvp in _activeAudios.ToArray())
            {
                var id = kvp.Key;
                var activeAudio = kvp.Value;

                activeAudio.SourceController.OnEnded -= OnEnded;

                // Since OnEnded is unsubscribed, we release ducking manually.
                _duckingManager?.Release(id);

                activeAudio.SourceController.Stop();
                _sourceControllerFactory.Release(activeAudio.SourceController);
            }

            _activeAudios.Clear();

            if (_duckingManager is IDisposable disposableDuckingManager)
                disposableDuckingManager.Dispose();

            foreach (var controller in _mixerGroupControllers.Values)
            {
                if (controller is IDisposable disposable)
                    disposable.Dispose();
            }

            _mixerGroupControllers.Clear();
            _mixerGroups.Clear();

            _clipSelector?.Clear();
            _clipSelector = null;

            _duckingManager = null;

            SceneManager.sceneLoaded -= OnSceneLoad;
        }
    }
}