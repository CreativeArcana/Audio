using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;

namespace CreativeArcana.Audio
{
    public class AudioMixerGroupController : IAudioMixerGroupController, IDisposable
    {
        public AudioMixerGroup MixerGroup => _audioMixerGroup;
        public bool IsMuted => _isMute;

        private readonly AudioMixer _audioMixer;
        private readonly AudioMixerGroup _audioMixerGroup;
        private readonly string _volumeParamName;

        private float _finalVolume = 1f;
        private float _baseVolume = 1f;
        private float _volumeMultiplier = 1f;
        private float _duckingMultiplier = 1f;

        private Tween _volumeTween;
        private Tween _duckingTween;

        private bool _isMute;
        
        private string SaveKey => $"CreativeArcana.Audio.{_audioMixer.name}.{_volumeParamName}";
        
        public AudioMixerGroupController(AudioMixer audioMixer, string groupName, string volumeParamName)
        {
            _audioMixer = audioMixer;
            _audioMixerGroup = GetAudioMixerGroup(audioMixer, groupName);
            _volumeParamName = volumeParamName;
        }

        /// <summary>
        /// for audio settings (ex.in UI Settings)
        /// </summary>
        public void SetBaseVolume(float volume)
        {
            _baseVolume = Mathf.Clamp01(volume);
            UpdateAudioMixerVolume();
        }

        /// <summary>
        /// For duck, crossfade
        /// </summary>
        /// <param name="multiplier">final volume = baseVolume * multiplier</param>
        /// <param name="fadeDuration"></param>
        public void SetVolumeMultiplier(float multiplier, float fadeDuration = 0)
        {
            multiplier = Mathf.Clamp01(multiplier);
            
            if (fadeDuration <= 0f)
            {
                _volumeMultiplier = multiplier;
                UpdateAudioMixerVolume();
                return;
            }

            _volumeTween?.Kill();

            _volumeTween = DOTween.To(
                () => _volumeMultiplier,
                x =>
                {
                    _volumeMultiplier = x;
                    UpdateAudioMixerVolume();
                },
                multiplier,
                fadeDuration
            ).SetEase(Ease.Linear);
        }

        public void SetDuckingMultiplier(float multiplier, float fadeDuration = 0)
        {
            multiplier = Mathf.Clamp01(multiplier);
            
            if (fadeDuration <= 0f)
            {
                _duckingMultiplier = multiplier;
                UpdateAudioMixerVolume();
                return;
            }

            _duckingTween?.Kill();

            _duckingTween = DOTween.To(
                () => _duckingMultiplier,
                x =>
                {
                    _duckingMultiplier = x;
                    UpdateAudioMixerVolume();
                },
                multiplier,
                fadeDuration
            ).SetEase(Ease.Linear);
        }

        public void SaveVolume()
        {
            PlayerPrefs.SetFloat(SaveKey, _baseVolume);
            PlayerPrefs.Save();
        }

        public float LoadVolume()
        {
            return PlayerPrefs.GetFloat(SaveKey, 1f);
        }

        public float GetEffectiveVolume()
        {
            return _isMute ? 0f : _finalVolume;
        }

        public void MuteChannel(bool mute)
        {
            if (mute)
            {
                _isMute = true;
                SetMixerVolumeDb(-80f);
            }
            else
            {
                _isMute = false;
                UpdateAudioMixerVolume();
            }
        }

        private void UpdateAudioMixerVolume()
        {
            _finalVolume = Mathf.Clamp01(_baseVolume * _volumeMultiplier * _duckingMultiplier);
            
            if(_isMute)
                return;
            
            if (_finalVolume <= 0.0001f)
                SetMixerVolumeDb(-80f);
            else
                SetMixerVolumeDb(Mathf.Log10(_finalVolume) * 20f);
        }

        private AudioMixerGroup GetAudioMixerGroup(AudioMixer audioMixer, string groupName)
        {
            if (audioMixer == null)
                throw new ArgumentNullException(nameof(audioMixer));

            var groups = audioMixer.FindMatchingGroups(groupName);

            if (groups == null || groups.Length == 0)
            {
                throw new InvalidOperationException(
                    $"AudioMixerGroup '{groupName}' not found in mixer '{audioMixer.name}'.");
            }

            return groups[0];
        }

        private void SetMixerVolumeDb(float db)
        {
            if (!_audioMixer.SetFloat(_volumeParamName, db))
            {
                Debug.LogError($"[AudioMixerGroupController] Mixer parameter not found or not exposed: {_volumeParamName}");
            }
        }
        
        public void Dispose()
        {
            _volumeTween?.Kill();
            _duckingTween?.Kill();
            _volumeTween = null;
            _duckingTween = null;
        }
    }
}