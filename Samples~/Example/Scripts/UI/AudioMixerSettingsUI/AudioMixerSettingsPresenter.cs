using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace CreativeArcana.Audio.Example
{
    public class AudioMixerSettingsPresenter : MonoBehaviour
    {
        [SerializeField] private List<MixerGroupSettingsUI> _settings;
        [SerializeField] private Button _saveButton;

        private IAudioService _audioService;

        private void Start()
        {
            _audioService = AudioManager.Instance.AudioService;
            LoadAndSetMixerGroupUI();
        }

        private void OnEnable()
        {
            foreach (var setting in _settings)
            {
                setting.OnValueChanged += OnMixerGroupSettingChanged;
            }

            _saveButton.onClick.AddListener(OnSaveButtonClicked);
        }

        private void OnDisable()
        {
            foreach (var setting in _settings)
            {
                setting.OnValueChanged -= OnMixerGroupSettingChanged;
            }
        }
        
        private void LoadAndSetMixerGroupUI()
        {
            foreach (var setting in _settings)
            {
                var loadedValue=_audioService.LoadChannelVolume(setting.AudioChannel);
                setting.SetSliderValue(loadedValue);
                setting.SetVolumeText(loadedValue.ToString(CultureInfo.InvariantCulture));
            }
        }
        
        private void OnMixerGroupSettingChanged(AudioChannel channel, float value)
        {
            _audioService.SetChannelBaseVolume(channel, value);
        }

        private void OnSaveButtonClicked()
        {
            foreach (var setting in _settings)
            {
                _audioService.SaveChannelVolume(setting.AudioChannel);
            }
        }
    }
}