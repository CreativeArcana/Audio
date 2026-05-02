using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CreativeArcana.Audio.Example
{
    public class ResetScenePresenter : MonoBehaviour
    {
        [SerializeField] private Button _resetButton;

        private IAudioService _audioService;
        
        private void Start()
        {
            _audioService = AudioManager.Instance.AudioService;
        }

        private void OnEnable()
        {
            _resetButton.onClick.AddListener(OnResetButtonClicked);
        }
        

        private void OnDisable()
        {
            _resetButton.onClick.RemoveListener(OnResetButtonClicked);
        }
        
        private void OnResetButtonClicked()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
