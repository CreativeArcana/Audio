using System;

namespace CreativeArcana.Audio
{
    internal sealed class ActiveAudio
    {
        public IAudioSourceController SourceController => _sourceController;
        public AudioLifetime Lifetime => _lifetime;

        private readonly IAudioSourceController _sourceController;
        private AudioLifetime _lifetime;

        public ActiveAudio(IAudioSourceController sourceController, AudioLifetime lifetime)
        {
            _sourceController = sourceController ?? throw new ArgumentNullException(nameof(sourceController));
            _lifetime = lifetime;
        }

        public void SetLifetime(AudioLifetime lifetime)
        {
            _lifetime = lifetime;
        }
    }
}