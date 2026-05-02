using System;
using UnityEngine;

namespace CreativeArcana.Audio.Example
{
    public class MusicModel
    {
        public int Id;
        public AudioHandle AudioHandle;
    }

    public class MusicManager : MonoBehaviour
    {
        public static MusicManager Instance => _instance;
        private static MusicManager _instance;

        public MusicModel ActiveMusic => _activeMusic;
        private MusicModel _activeMusic;

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
        }

        private void Start()
        {
            _audioService = AudioManager.Instance.AudioService;
        }

        public void PlayMusic(int musicId, AudioLibraryId audioLibraryId, float fadeInDuration)
        {
            if (_activeMusic != null)
            {
                _audioService.Stop(_activeMusic.AudioHandle);
            }

            var audioHandle = _audioService.Play(audioLibraryId, fadeInDuration);
            //_audioService.SetLifetime(audioHandle, AudioLifetime.PersistentAcrossScenes);
            
            _activeMusic = new MusicModel { Id = musicId, AudioHandle = audioHandle };
        }

        public void CrossFadePlayMusic(int musicId, AudioLibraryId audioLibraryId, float fadeInDuration,float fadeOutDuration)
        {
            if (_activeMusic == null || _activeMusic.Id == musicId)
            {
                return;
            }
            _audioService.SetLifetime(_activeMusic.AudioHandle, AudioLifetime.SceneBound);
            
            var audioHandle = _audioService.CrossFadePlay(_activeMusic.AudioHandle,audioLibraryId ,fadeInDuration,fadeOutDuration);
            //_audioService.SetLifetime(audioHandle, AudioLifetime.PersistentAcrossScenes);
            
            _activeMusic = new MusicModel { Id = musicId, AudioHandle = audioHandle };
        }
        
        public void StopMusic(int musicId)
        {
            if (_activeMusic == null)
                return;
            
            if (!_activeMusic.Id.Equals(musicId))
                return;

            _audioService.Stop(_activeMusic.AudioHandle);
            _activeMusic = null;
        }

        public void PauseMusic(int musicId)
        {
            if (_activeMusic == null)
                return;
            
            if (!_activeMusic.Id.Equals(musicId))
                return;

            _audioService.Pause(_activeMusic.AudioHandle);
        }

        public void ResumeMusic(int musicId)
        {
            if (_activeMusic == null)
                return;
            
            if (!_activeMusic.Id.Equals(musicId))
                return;

            _audioService.Resume(_activeMusic.AudioHandle);
        }
    }
}