using UnityEngine;
using UnityEngine.UI;

namespace CreativeArcana.Audio.Example
{
    public class DuckingPlaybackUI : AudioPlaybackUI
    {
        [SerializeField] private Toggle _toggle;
        [SerializeField] private DuckingProfile _duckingProfile;

        protected override void OnPlayButtonClicked()
        {
            if (!_toggle.isOn)
            {
                base.OnPlayButtonClicked();
                
            }
            else
            {
                _audioHandle = _audioService.Play(_audioId, FadeInDuration, _duckingProfile);
            }
        }
    }
}
