using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Audio;

namespace CreativeArcana.Audio.Tests
{
    public class AudioSourceControllerTests
    {
        private GameObject _go;
        private AudioSourceController _controller;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("AudioSourceControllerTestObject");
            _go.AddComponent<AudioSource>();
            _controller = _go.AddComponent<AudioSourceController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
                Object.DestroyImmediate(_go);
        }

        [Test]
        public void Play_WithoutInitialize_ShouldThrow()
        {
            var entry = ScriptableObject.CreateInstance<AudioEntry>();
            var clip = AudioClip.Create("test", 1000, 1, 44100, false);

            Assert.Throws<System.InvalidOperationException>(() =>
            {
                _controller.Play(new AudioHandle(1), entry, clip);
            });
        }

        [Test]
        public void Initialize_WithNullMixerGroups_ShouldThrow()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
            {
                _controller.Initialize(null);
            });
        }

        [Test]
        public void Set2D_ShouldApply2DSettings()
        {
            _controller.Initialize(new Dictionary<AudioChannel, AudioMixerGroup>());
            
            _controller.Set2D();

            var source = _go.GetComponent<AudioSource>();
            Assert.AreEqual(0f, source.spatialBlend);
            Assert.AreEqual(0f, source.dopplerLevel);
            Assert.AreEqual(0, source.spread);
        }

        [Test]
        public void Set3D_ShouldApply3DSettings()
        {
            _controller.Initialize(new Dictionary<AudioChannel, AudioMixerGroup>());
            
            _controller.Set3D(2f, 10f);

            var source = _go.GetComponent<AudioSource>();
            Assert.AreEqual(1f, source.spatialBlend);
            Assert.AreEqual(1f, source.dopplerLevel);
            Assert.AreEqual(0, source.spread);
            Assert.AreEqual(AudioRolloffMode.Logarithmic, source.rolloffMode);
            Assert.AreEqual(2f, source.minDistance);
            Assert.AreEqual(10f, source.maxDistance);
        }

        [Test]
        public void Set3D_ShouldClampMinAndMaxDistance()
        {
            _controller.Initialize(new Dictionary<AudioChannel, AudioMixerGroup>());
            
            _controller.Set3D(-1f, -2f);

            var source = _go.GetComponent<AudioSource>();
            Assert.AreEqual(0.01f, source.minDistance);
            Assert.AreEqual(0.01f, source.maxDistance);
        }

        [Test]
        public void SetPosition_ShouldMoveTransform()
        {
            var pos = new Vector3(10, 20, 30);

            _controller.SetPosition(pos);

            Assert.AreEqual(pos, _go.transform.position);
        }
        
        [Test]
        public void OnPoolRelease_ShouldResetState()
        {
            _controller.Initialize(new Dictionary<AudioChannel, AudioMixerGroup>());
            
            _controller.OnPoolCreate();
            _controller.OnPoolRelease();

            var source = _go.GetComponent<AudioSource>();
            Assert.IsNull(source.clip);
            Assert.AreEqual(1f, source.volume);
            Assert.AreEqual(1f, source.pitch);
            Assert.IsFalse(source.loop);
            Assert.AreEqual(0f, source.spatialBlend);
        }

        [Test]
        public void IsPlaying_WhenNothingPlaying_ShouldReturnFalse()
        {
            Assert.IsFalse(_controller.IsPlaying());
        }
    }
}
