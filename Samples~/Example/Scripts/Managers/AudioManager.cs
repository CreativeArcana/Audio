using CreativeArcana.Factory;
using UnityEngine;
using UnityEngine.Audio;

namespace CreativeArcana.Audio.Example
{
    public class AudioManager : MonoBehaviour
    {
        [SerializeField] private AudioMixer _audioMixer;
        [SerializeField] private AudioLibrary _audioLibrary;
        [SerializeField] private AudioSourceController _audioSourceControllerPrefab;

        public static AudioManager Instance => _instance;
        public IAudioService AudioService => _audioService;
        
        private static AudioManager _instance;
        private IAudioService _audioService;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            StartAudioService();
        }

        private void StartAudioService()
        {
            var audioSourceControllerFactory =
                new ComponentPoolFactory<AudioSourceController>(
                    _audioSourceControllerPrefab,
                    transform,
                    new PoolSettings()
                    {
                        DefaultCapacity = 20,
                        PreWarmCount = 10,
                    }
                ).AsInterface<AudioSourceController, IAudioSourceController>();
            
            _audioService = new AudioService(_audioMixer, _audioLibrary, audioSourceControllerFactory);
        }
    }
}