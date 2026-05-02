using System.Collections.Generic;
using UnityEngine;

namespace CreativeArcana.Audio
{
    [CreateAssetMenu(menuName = "CreativeArcana/Audio/Data/AudioEntry", fileName = "AudioEntry")]
    public class AudioEntry : ScriptableObject
    {
        #region SerializeFields

        [Header("Main")] [SerializeField] private AudioId _id;
        [SerializeField] private AudioClip[] _clips;
        [SerializeField] private AudioPlaybackMode _playbackMode;
        [SerializeField] private AudioChannel _audioChannel = AudioChannel.Master;
        [SerializeField] private AudioLifetime _lifetime = AudioLifetime.SceneBound;

        [Header("Volume & Pitch")] [Range(0f, 1f)] [SerializeField]
        private float _minVolume = 1f;

        [Range(0f, 1f)] [SerializeField] private float _maxVolume = 1f;
        [Range(-3f, 3f)] [SerializeField] private float _minPitch = 1f;
        [Range(-3f, 3f)] [SerializeField] private float _maxPitch = 1f;

        [Header("Playback Options")] [SerializeField]
        private bool _loop;

        [Tooltip("If true, plays in 3D space using spatial blend.")] [SerializeField]
        private bool _spatial;

        [Header("3D Sound Settings")] [Range(0f, 1f)] [SerializeField]
        private float _spatialBlend = 1f;

        [Range(0f, 5f)] [SerializeField] private float _dopplerLevel = 1f;
        [Range(0f, 360f)] [SerializeField] private int _spread;
        [SerializeField] private AudioRolloffMode _rolloffMode = AudioRolloffMode.Logarithmic;
        [SerializeField] private float _minDistance = 1f;
        [SerializeField] private float _maxDistance = 500f;

        #endregion

        #region Properties

        public IReadOnlyList<AudioClip> Clips => _clips;
        public AudioPlaybackMode PlaybackMode => _playbackMode;
        public AudioId Id => _id;
        public AudioChannel AudioChannel => _audioChannel;
        public AudioLifetime Lifetime => _lifetime;
        public bool Loop => _loop;
        public bool Spatial => _spatial;
        public float SpatialBlend => _spatialBlend;
        public float DopplerLevel => _dopplerLevel;
        public int Spread => _spread;
        public AudioRolloffMode RolloffMode => _rolloffMode;

        #endregion

        #region Public Methods

        public float GetVolume()
        {
            return Random.Range(_minVolume, _maxVolume);
        }

        public float GetPitch()
        {
            return Random.Range(_minPitch, _maxPitch);
        }

        public float MinDistance => Mathf.Max(0.01f, _minDistance);
        public float MaxDistance => Mathf.Max(MinDistance, _maxDistance);

        #endregion

        #region Validate Editor

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (_maxVolume < _minVolume)
                _maxVolume = _minVolume;

            if (_maxPitch < _minPitch)
                _maxPitch = _minPitch;

            if (_maxDistance < _minDistance)
                _maxDistance = _minDistance;

            if (string.IsNullOrWhiteSpace(_id.Value))
            {
                var path = UnityEditor.AssetDatabase.GetAssetPath(this);
                if (!string.IsNullOrEmpty(path))
                {
                    var filName = System.IO.Path.GetFileNameWithoutExtension(path);
                    _id.SetId(filName);
                }
            }
#endif
        }

        #endregion
    }
}