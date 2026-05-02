using System.Collections.Generic;
using NUnit.Framework;

namespace CreativeArcana.Audio.Tests.Editor
{
    public class DuckingManagerTests
    {
        private class SetCall
        {
            public AudioChannel Channel;
            public float Multiplier;
            public float FadeDuration;
        }

        private List<SetCall> _calls;
        private DuckingManager _manager;

        [SetUp]
        public void SetUp()
        {
            _calls = new List<SetCall>();
            _manager = new DuckingManager((channel, multiplier, fadeDuration) =>
            {
                _calls.Add(new SetCall
                {
                    Channel = channel,
                    Multiplier = multiplier,
                    FadeDuration = fadeDuration
                });
            });
        }

        [Test]
        public void Apply_WithAudioIdZero_ShouldDoNothing()
        {
            var profile = new DuckingProfile(true, new List<AudioChannel> { AudioChannel.Music }, 0.5f, 0.2f, 0.3f);

            _manager.Apply(0, profile);

            Assert.AreEqual(0, _calls.Count);
        }

        [Test]
        public void Apply_WithDisabledProfile_ShouldDoNothing()
        {
            var profile = new DuckingProfile(false, new List<AudioChannel> { AudioChannel.Music }, 0.5f, 0.2f, 0.3f);

            _manager.Apply(1, profile);

            Assert.AreEqual(0, _calls.Count);
        }

        [Test]
        public void Apply_WithNullChannels_ShouldDoNothing()
        {
            var profile = new DuckingProfile(true, null, 0.5f, 0.2f, 0.3f);

            _manager.Apply(1, profile);

            Assert.AreEqual(0, _calls.Count);
        }

        [Test]
        public void Apply_ShouldSetDuckMultiplier_ForEachUniqueChannel()
        {
            var profile = new DuckingProfile(
                true,
                new List<AudioChannel> { AudioChannel.Music, AudioChannel.Music, AudioChannel.SFX },
                0.4f,
                0.25f,
                0.75f);

            _manager.Apply(10, profile);

            Assert.AreEqual(2, _calls.Count);

            Assert.AreEqual(AudioChannel.Music, _calls[0].Channel);
            Assert.AreEqual(0.4f, _calls[0].Multiplier);
            Assert.AreEqual(0.25f, _calls[0].FadeDuration);

            Assert.AreEqual(AudioChannel.SFX, _calls[1].Channel);
            Assert.AreEqual(0.4f, _calls[1].Multiplier);
            Assert.AreEqual(0.25f, _calls[1].FadeDuration);
        }

        [Test]
        public void Release_WhenLastActiveOnChannel_ShouldRestoreToOne()
        {
            var profile = new DuckingProfile(
                true,
                new List<AudioChannel> { AudioChannel.Music },
                0.4f,
                0.2f,
                0.8f);

            _manager.Apply(1, profile);
            _calls.Clear();

            _manager.Release(1);

            Assert.AreEqual(1, _calls.Count);
            Assert.AreEqual(AudioChannel.Music, _calls[0].Channel);
            Assert.AreEqual(1f, _calls[0].Multiplier);
            Assert.AreEqual(0.8f, _calls[0].FadeDuration);
        }

        [Test]
        public void Release_WhenOlderProfileRemoved_ShouldKeepNewestProfile()
        {
            var oldProfile = new DuckingProfile(
                true,
                new List<AudioChannel> { AudioChannel.Music },
                0.6f,
                0.1f,
                0.3f);

            var newProfile = new DuckingProfile(
                true,
                new List<AudioChannel> { AudioChannel.Music },
                0.2f,
                0.4f,
                0.9f);

            _manager.Apply(1, oldProfile);
            _manager.Apply(2, newProfile);
            _calls.Clear();

            _manager.Release(1);

            Assert.AreEqual(1, _calls.Count);
            Assert.AreEqual(AudioChannel.Music, _calls[0].Channel);
            Assert.AreEqual(0.2f, _calls[0].Multiplier);
            Assert.AreEqual(0.4f, _calls[0].FadeDuration);
        }

        [Test]
        public void Release_WhenNewestRemoved_ShouldFallbackToPreviousProfile()
        {
            var oldProfile = new DuckingProfile(
                true,
                new List<AudioChannel> { AudioChannel.Music },
                0.6f,
                0.1f,
                0.3f);

            var newProfile = new DuckingProfile(
                true,
                new List<AudioChannel> { AudioChannel.Music },
                0.2f,
                0.4f,
                0.9f);

            _manager.Apply(1, oldProfile);
            _manager.Apply(2, newProfile);
            _calls.Clear();

            _manager.Release(2);

            Assert.AreEqual(1, _calls.Count);
            Assert.AreEqual(AudioChannel.Music, _calls[0].Channel);
            Assert.AreEqual(0.6f, _calls[0].Multiplier);
            Assert.AreEqual(0.1f, _calls[0].FadeDuration);
        }

        [Test]
        public void Pause_ShouldRemoveActiveDuck_AndRestoreChannel()
        {
            var profile = new DuckingProfile(
                true,
                new List<AudioChannel> { AudioChannel.UI },
                0.3f,
                0.2f,
                0.7f);

            _manager.Apply(7, profile);
            _calls.Clear();

            _manager.Pause(7);

            Assert.AreEqual(1, _calls.Count);
            Assert.AreEqual(AudioChannel.UI, _calls[0].Channel);
            Assert.AreEqual(1f, _calls[0].Multiplier);
            Assert.AreEqual(0.7f, _calls[0].FadeDuration);
        }

        [Test]
        public void Resume_ShouldReapplyDuckMultiplier()
        {
            var profile = new DuckingProfile(
                true,
                new List<AudioChannel> { AudioChannel.UI },
                0.3f,
                0.2f,
                0.7f);

            _manager.Apply(7, profile);
            _manager.Pause(7);
            _calls.Clear();

            _manager.Resume(7);

            Assert.AreEqual(1, _calls.Count);
            Assert.AreEqual(AudioChannel.UI, _calls[0].Channel);
            Assert.AreEqual(0.3f, _calls[0].Multiplier);
            Assert.AreEqual(0.2f, _calls[0].FadeDuration);
        }

        [Test]
        public void Clear_ShouldResetAllActiveChannelsToOne()
        {
            var profile1 = new DuckingProfile(
                true,
                new List<AudioChannel> { AudioChannel.Music },
                0.4f,
                0.1f,
                0.2f);

            var profile2 = new DuckingProfile(
                true,
                new List<AudioChannel> { AudioChannel.SFX },
                0.5f,
                0.3f,
                0.6f);

            _manager.Apply(1, profile1);
            _manager.Apply(2, profile2);
            _calls.Clear();

            _manager.Clear();

            Assert.AreEqual(2, _calls.Count);
            Assert.That(_calls.Exists(x => x.Channel == AudioChannel.Music && x.Multiplier == 1f));
            Assert.That(_calls.Exists(x => x.Channel == AudioChannel.SFX && x.Multiplier == 1f));
        }
    }
}
