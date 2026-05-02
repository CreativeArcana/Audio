using System;
using UnityEngine;
using UnityEngine.UI;

namespace CreativeArcana.Audio.Example
{
    public class MusicPlaybackUI : AudioPlaybackUI
    {
        [SerializeField] private int _id;
        [SerializeField] private Button _crossFadeButton;

        private void Awake()
        {
            _crossFadeButton.interactable = false;
        }

        private void LateUpdate()
        {
            if (MusicManager.Instance.ActiveMusic != null &&
                MusicManager.Instance.ActiveMusic.Id != _id)
            {
                _crossFadeButton.interactable = true;
            }
            else
            {
                _crossFadeButton.interactable = false;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _crossFadeButton.onClick.AddListener(OnCrossFadeButtonClicked);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _crossFadeButton.onClick.RemoveListener(OnCrossFadeButtonClicked);
        }

        protected override void OnPlayButtonClicked()
        {
            MusicManager.Instance.PlayMusic(_id, _audioId, FadeInDuration);
        }

        protected override void OnStopButtonClicked()
        {
            MusicManager.Instance.StopMusic(_id);
        }

        protected override void OnPauseButtonClicked()
        {
            MusicManager.Instance.PauseMusic(_id);
        }

        protected override void OnResumeButtonClicked()
        {
            MusicManager.Instance.ResumeMusic(_id);
        }

        private void OnCrossFadeButtonClicked()
        {
            MusicManager.Instance.CrossFadePlayMusic(_id, _audioId, FadeInDuration, FadeOutDuration);
        }
    }
}