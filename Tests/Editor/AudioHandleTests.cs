using NUnit.Framework;

namespace CreativeArcana.Audio.Tests.Editor
{
    public class AudioHandleTests
    {
        [Test]
        public void Invalid_ShouldHaveIdZero_AndBeInvalid()
        {
            var handle = AudioHandle.Invalid;

            Assert.AreEqual(0, handle.Id);
            Assert.IsFalse(handle.IsValid);
            Assert.AreEqual("Invalid", handle.ToString());
        }

        [Test]
        public void Constructor_WithPositiveId_ShouldBeValid()
        {
            var handle = new AudioHandle(10);

            Assert.AreEqual(10, handle.Id);
            Assert.IsTrue(handle.IsValid);
            Assert.AreEqual("10", handle.ToString());
        }

        [Test]
        public void Equals_WithSameId_ShouldBeTrue()
        {
            var a = new AudioHandle(5);
            var b = new AudioHandle(5);

            Assert.IsTrue(a.Equals(b));
            Assert.IsTrue(a == b);
            Assert.IsFalse(a != b);
        }

        [Test]
        public void Equals_WithDifferentId_ShouldBeFalse()
        {
            var a = new AudioHandle(5);
            var b = new AudioHandle(6);

            Assert.IsFalse(a.Equals(b));
            Assert.IsFalse(a == b);
            Assert.IsTrue(a != b);
        }

        [Test]
        public void GetHashCode_ShouldMatchIdHashCode()
        {
            var handle = new AudioHandle(42);

            Assert.AreEqual(42.GetHashCode(), handle.GetHashCode());
        }
    }
}