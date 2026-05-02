using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CreativeArcana.Audio.Tests
{
    public class AudioSourceControllerPlayModeTests
    {
        //TODO: need more tests
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
        
        [UnityTest]
        public IEnumerator SetFollowTarget_ShouldUpdatePositionInLateUpdate()
        {
            var target = new GameObject("Target");
            target.transform.position = new Vector3(5, 6, 7);

            _controller.SetFollowTarget(target.transform);

            yield return null;

            Assert.AreEqual(target.transform.position, _go.transform.position);

            Object.DestroyImmediate(target);
        }
    }
}
