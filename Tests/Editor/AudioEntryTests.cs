using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CreativeArcana.Audio.Tests.Editor
{
    public class AudioEntryTests
    {
        //TODO: Don't use reflection in tests [AudioEntryTests]
        private AudioEntry CreateEntry()
        {
            return ScriptableObject.CreateInstance<AudioEntry>();
        }

        private void SetPrivateField<T>(object obj, string fieldName, T value)
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"Field not found: {fieldName}");
            field.SetValue(obj, value);
        }

        [Test]
        public void MinDistance_ShouldBeAtLeast_0_01()
        {
            var entry = CreateEntry();
            SetPrivateField(entry, "_minDistance", -5f);

            Assert.AreEqual(0.01f, entry.MinDistance);
        }

        [Test]
        public void MaxDistance_ShouldBeAtLeast_MinDistance()
        {
            var entry = CreateEntry();
            SetPrivateField(entry, "_minDistance", 10f);
            SetPrivateField(entry, "_maxDistance", 5f);

            Assert.AreEqual(10f, entry.MinDistance);
            Assert.AreEqual(10f, entry.MaxDistance);
        }

        [Test]
        public void Properties_ShouldReturnSerializedValues()
        {
            var entry = CreateEntry();
            var clips = new[]
            {
                AudioClip.Create("clip1", 1000, 1, 44100, false),
                AudioClip.Create("clip2", 1000, 1, 44100, false),
            };

            SetPrivateField(entry, "_clips", clips);
            SetPrivateField(entry, "_playbackMode", AudioPlaybackMode.Shuffle);
            SetPrivateField(entry, "_audioChannel", AudioChannel.Voice);
            SetPrivateField(entry, "_loop", true);
            SetPrivateField(entry, "_spatial", true);
            SetPrivateField(entry, "_spatialBlend", 0.8f);
            SetPrivateField(entry, "_dopplerLevel", 2f);
            SetPrivateField(entry, "_spread", 180);
            SetPrivateField(entry, "_rolloffMode", AudioRolloffMode.Linear);

            Assert.AreEqual(2, entry.Clips.Count);
            Assert.AreEqual(AudioPlaybackMode.Shuffle, entry.PlaybackMode);
            Assert.AreEqual(AudioChannel.Voice, entry.AudioChannel);
            Assert.IsTrue(entry.Loop);
            Assert.IsTrue(entry.Spatial);
            Assert.AreEqual(0.8f, entry.SpatialBlend);
            Assert.AreEqual(2f, entry.DopplerLevel);
            Assert.AreEqual(180, entry.Spread);
            Assert.AreEqual(AudioRolloffMode.Linear, entry.RolloffMode);
        }

        [Test]
        public void GetVolume_WhenMinEqualsMax_ShouldReturnSameValue()
        {
            var entry = CreateEntry();
            SetPrivateField(entry, "_minVolume", 0.65f);
            SetPrivateField(entry, "_maxVolume", 0.65f);

            Assert.AreEqual(0.65f, entry.GetVolume(),0.001f);
        }

        [Test]
        public void GetPitch_WhenMinEqualsMax_ShouldReturnSameValue()
        {
            var entry = CreateEntry();
            SetPrivateField(entry, "_minPitch", 1.25f);
            SetPrivateField(entry, "_maxPitch", 1.25f);

            Assert.AreEqual(1.25f, entry.GetPitch());
        }
    }
}
