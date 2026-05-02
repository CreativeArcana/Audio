using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CreativeArcana.Audio.Tests.Editor
{
    public class AudioClipSelectorTests
    {
        //TODO: Don't use reflection in tests [AudioClipSelectorTests]
        private AudioEntry CreateEntry(AudioPlaybackMode mode, params AudioClip[] clips)
        {
            var entry = ScriptableObject.CreateInstance<AudioEntry>();
            SetPrivateField(entry, "_playbackMode", mode);
            SetPrivateField(entry, "_clips", clips);
            return entry;
        }

        private AudioClip CreateClip(string name)
        {
            return AudioClip.Create(name, 1000, 1, 44100, false);
        }

        private void SetPrivateField<T>(object obj, string fieldName, T value)
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"Field not found: {fieldName}");
            field.SetValue(obj, value);
        }

        [Test]
        public void GetClip_WhenEntryIsNull_ShouldReturnNull()
        {
            var selector = new AudioClipSelector();

            var result = selector.GetClip(null);

            Assert.IsNull(result);
        }

        [Test]
        public void GetClip_WhenClipsAreNull_ShouldReturnNull()
        {
            var entry = ScriptableObject.CreateInstance<AudioEntry>();
            SetPrivateField<object>(entry, "_clips", null);
            SetPrivateField(entry, "_playbackMode", AudioPlaybackMode.Single);

            var selector = new AudioClipSelector();

            var result = selector.GetClip(entry);

            Assert.IsNull(result);
        }

        [Test]
        public void GetClip_WhenClipsAreEmpty_ShouldReturnNull()
        {
            var entry = CreateEntry(AudioPlaybackMode.Single);
            var selector = new AudioClipSelector();

            var result = selector.GetClip(entry);

            Assert.IsNull(result);
        }

        [Test]
        public void Single_ShouldAlwaysReturnFirstClip()
        {
            var clip1 = CreateClip("clip1");
            var clip2 = CreateClip("clip2");
            var entry = CreateEntry(AudioPlaybackMode.Single, clip1, clip2);
            var selector = new AudioClipSelector();

            var result1 = selector.GetClip(entry);
            var result2 = selector.GetClip(entry);
            var result3 = selector.GetClip(entry);

            Assert.AreSame(clip1, result1);
            Assert.AreSame(clip1, result2);
            Assert.AreSame(clip1, result3);
        }

        [Test]
        public void Random_WithSingleClip_ShouldReturnThatClip()
        {
            var clip = CreateClip("clip1");
            var entry = CreateEntry(AudioPlaybackMode.Random, clip);
            var selector = new AudioClipSelector();

            var result = selector.GetClip(entry);

            Assert.AreSame(clip, result);
        }

        [Test]
        public void RandomNoRepeat_WithSingleClip_ShouldReturnThatClip()
        {
            var clip = CreateClip("clip1");
            var entry = CreateEntry(AudioPlaybackMode.RandomNoRepeat, clip);
            var selector = new AudioClipSelector();

            var result1 = selector.GetClip(entry);
            var result2 = selector.GetClip(entry);

            Assert.AreSame(clip, result1);
            Assert.AreSame(clip, result2);
        }

        [Test]
        public void RandomNoRepeat_WithTwoClips_ShouldNotRepeatImmediately()
        {
            var clip1 = CreateClip("clip1");
            var clip2 = CreateClip("clip2");
            var entry = CreateEntry(AudioPlaybackMode.RandomNoRepeat, clip1, clip2);
            var selector = new AudioClipSelector();

            var previous = selector.GetClip(entry);

            for (int i = 0; i < 20; i++)
            {
                var current = selector.GetClip(entry);
                Assert.AreNotSame(previous, current);
                previous = current;
            }
        }

        [Test]
        public void Sequential_ShouldReturnClipsInOrder_AndLoop()
        {
            var clip1 = CreateClip("clip1");
            var clip2 = CreateClip("clip2");
            var clip3 = CreateClip("clip3");

            var entry = CreateEntry(AudioPlaybackMode.Sequential, clip1, clip2, clip3);
            var selector = new AudioClipSelector();

            Assert.AreSame(clip1, selector.GetClip(entry));
            Assert.AreSame(clip2, selector.GetClip(entry));
            Assert.AreSame(clip3, selector.GetClip(entry));
            Assert.AreSame(clip1, selector.GetClip(entry));
            Assert.AreSame(clip2, selector.GetClip(entry));
        }

        [Test]
        public void Shuffle_WithSingleClip_ShouldReturnThatClip()
        {
            var clip = CreateClip("clip1");
            var entry = CreateEntry(AudioPlaybackMode.Shuffle, clip);
            var selector = new AudioClipSelector();

            var result1 = selector.GetClip(entry);
            var result2 = selector.GetClip(entry);

            Assert.AreSame(clip, result1);
            Assert.AreSame(clip, result2);
        }

        [Test]
        public void Shuffle_WithMultipleClips_ShouldReturnAllClipsBeforeRepeatingBag()
        {
            var clip1 = CreateClip("clip1");
            var clip2 = CreateClip("clip2");
            var clip3 = CreateClip("clip3");

            var entry = CreateEntry(AudioPlaybackMode.Shuffle, clip1, clip2, clip3);
            var selector = new AudioClipSelector();

            var round = new HashSet<AudioClip>
            {
                selector.GetClip(entry),
                selector.GetClip(entry),
                selector.GetClip(entry)
            };

            Assert.AreEqual(3, round.Count);
            Assert.Contains(clip1, new List<AudioClip>(round));
            Assert.Contains(clip2, new List<AudioClip>(round));
            Assert.Contains(clip3, new List<AudioClip>(round));
        }

        [Test]
        public void Clear_ShouldResetInternalState()
        {
            var clip1 = CreateClip("clip1");
            var clip2 = CreateClip("clip2");
            var clip3 = CreateClip("clip3");

            var entry = CreateEntry(AudioPlaybackMode.Sequential, clip1, clip2, clip3);
            var selector = new AudioClipSelector();

            Assert.AreSame(clip1, selector.GetClip(entry));
            Assert.AreSame(clip2, selector.GetClip(entry));

            selector.Clear();

            Assert.AreSame(clip1, selector.GetClip(entry));
        }
    }
}
