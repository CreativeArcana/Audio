using System;
using System.Collections.Generic;
using UnityEngine;

namespace CreativeArcana.Audio
{
    [Serializable]
    public struct DuckingProfile
    {
        [SerializeField] private bool _enabled;
        [SerializeField] private List<AudioChannel> _duckChannels;
        [SerializeField] private float _duckVolume;
        [SerializeField] private float _fadeInDuration;
        [SerializeField] private float _fadeOutDuration;
        
        public bool Enabled => _enabled;
        public IReadOnlyCollection<AudioChannel> DuckChannels => _duckChannels;
        public float DuckVolume => _duckVolume;
        public float FadeInDuration => _fadeInDuration;
        public float FadeOutDuration => _fadeOutDuration;
        
        //TODO: add priority
        
        public DuckingProfile(bool enabled, List<AudioChannel> duckChannels, float duckVolume,
            float fadeInDuration , float fadeOutDuration)
        {
            _enabled = enabled;
            _duckChannels = duckChannels;
            _duckVolume = duckVolume;
            _fadeInDuration = fadeInDuration;
            _fadeOutDuration = fadeOutDuration;
        }
    }
}