using NUnit.Framework;

namespace CreativeArcana.Audio.Tests.Editor
{
    public class AudioIdTests
    {
        [Test]
        public void Constructor_ShouldStoreValue()
        {
            var id = new AudioId("music_theme");

            Assert.AreEqual("music_theme", id.Value);
            Assert.AreEqual("music_theme", id.ToString());
        }

        [Test]
        public void Equals_WithSameValue_ShouldBeTrue()
        {
            var a = new AudioId("click");
            var b = new AudioId("click");

            Assert.IsTrue(a.Equals(b));
            Assert.IsTrue(a == b);
            Assert.IsFalse(a != b);
        }

        [Test]
        public void Equals_WithDifferentValue_ShouldBeFalse()
        {
            var a = new AudioId("click");
            var b = new AudioId("explosion");

            Assert.IsFalse(a.Equals(b));
            Assert.IsFalse(a == b);
            Assert.IsTrue(a != b);
        }

        [Test]
        public void GetHashCode_SameValue_ShouldBeEqual()
        {
            var a = new AudioId("voice_line");
            var b = new AudioId("voice_line");

            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Test]
        public void DefaultAudioId_ShouldHaveNullValue()
        {
            var id = default(AudioId);

            Assert.IsNull(id.Value);
        }
    }
}