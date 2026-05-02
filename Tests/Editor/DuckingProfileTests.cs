using System.Collections.Generic;
using NUnit.Framework;

namespace CreativeArcana.Audio.Tests.Editor
{
    public class DuckingProfileTests
    {
        [Test]
        public void Constructor_ShouldAssignAllProperties()
        {
            var channels = new List<AudioChannel>
            {
                AudioChannel.Music,
                AudioChannel.SFX
            };

            var profile = new DuckingProfile(
                true,
                channels,
                0.35f,
                0.5f,
                1.25f
            );

            Assert.IsTrue(profile.Enabled);
            Assert.AreEqual(channels, profile.DuckChannels);
            Assert.AreEqual(0.35f, profile.DuckVolume);
            Assert.AreEqual(0.5f, profile.FadeInDuration);
            Assert.AreEqual(1.25f, profile.FadeOutDuration);
        }

        [Test]
        public void DefaultProfile_ShouldHaveDefaultValues()
        {
            var profile = default(DuckingProfile);

            Assert.IsFalse(profile.Enabled);
            Assert.IsNull(profile.DuckChannels);
            Assert.AreEqual(0f, profile.DuckVolume);
            Assert.AreEqual(0f, profile.FadeInDuration);
            Assert.AreEqual(0f, profile.FadeOutDuration);
        }
    }
}