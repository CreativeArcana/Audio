using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CreativeArcana.Audio.Example
{
    public class AudioPlaybackUI : MonoBehaviour
    {
        [SerializeField] protected AudioLibraryId _audioId;
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _stopButton;
        [SerializeField] private TMP_InputField _fadeInField;
        [SerializeField] private TMP_InputField _fadeOutField;

        protected IAudioService _audioService;
        protected AudioHandle _audioHandle;
        
        protected float FadeInDuration => float.TryParse(_fadeInField.text, out var value) ? value : 0f;
        protected float FadeOutDuration => float.TryParse(_fadeOutField.text, out var value) ? value : 0f;



        protected virtual void Start()
        {
            _audioService = AudioManager.Instance.AudioService;
        }

        protected virtual void OnEnable()
        {
            _playButton.onClick.AddListener(OnPlayButtonClicked);
            _pauseButton.onClick.AddListener(OnPauseButtonClicked);
            _resumeButton.onClick.AddListener(OnResumeButtonClicked);
            _stopButton.onClick.AddListener(OnStopButtonClicked);
        }

        protected virtual void OnDisable()
        {
            _playButton.onClick.RemoveListener(OnPlayButtonClicked);
            _pauseButton.onClick.RemoveListener(OnPauseButtonClicked);
            _resumeButton.onClick.RemoveListener(OnResumeButtonClicked);
            _stopButton.onClick.RemoveListener(OnStopButtonClicked);
        }

        protected virtual void OnPlayButtonClicked()
        {
            _audioService.Stop(_audioHandle, FadeOutDuration);

            _audioHandle = _audioService.Play(_audioId, FadeInDuration);
        }

        protected virtual void OnPauseButtonClicked()
        {
            _audioService.Pause(_audioHandle, FadeOutDuration);
        }

        protected virtual void OnResumeButtonClicked()
        {
            _audioService.Resume(_audioHandle, FadeInDuration);
        }

        protected virtual void OnStopButtonClicked()
        {
            _audioService.Stop(_audioHandle, FadeOutDuration);
        }
    }
}