using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CreativeArcana.Audio.Example
{
    public class MixerGroupSettingsUI : MonoBehaviour
    {
        [SerializeField] private AudioChannel _audioChannel;
        [SerializeField] private Slider _slider;
        [SerializeField] private TMP_Text _text;
        
        public AudioChannel AudioChannel => _audioChannel;
        public event Action<AudioChannel,float> OnValueChanged;
        
        private void Awake()
        {
            _slider.minValue = 0;
            _slider.maxValue = 1;
        }

        private void OnEnable()
        {
            _slider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        private void OnDisable()
        {
            _slider.onValueChanged.RemoveListener(OnSliderValueChanged);
        }

        public void SetVolumeText(string txt)
        {
            _text.text = txt;
        }
        
        public void SetSliderValue(float volume)
        {
            _slider.value = volume;
        }
        
        private void OnSliderValueChanged(float newValue)
        {
            _text.text = newValue.ToString(CultureInfo.InvariantCulture);
            OnValueChanged?.Invoke(_audioChannel,Mathf.Clamp01(newValue));
        }
    }
}
