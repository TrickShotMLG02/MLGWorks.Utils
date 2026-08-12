using MLGWorks.Utils.Patterns.Singletons;
using MLGWorks.Utils.Patterns;
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace MLGWorks.Utils.Tests.Patterns
{
    public class TestEvent : IEvent
    {
        public string Name => nameof(TestEvent);
        public int Value { get; set; }
    }

    public class SingletonTests
    {
        [ExecuteAlways]
        private class TestSingleton : Singleton<TestSingleton>
        { }

        private sealed class TestPureSingleton : PureSingleton<TestPureSingleton>
        {
            public TestPureSingleton()
            { }
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // destroy all instances of TestSingleton after each test
            foreach (var obj in Object.FindObjectsOfType<TestSingleton>())
            {
                Object.DestroyImmediate(obj.gameObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Instance_CreatesAndFindsSingleton()
        {
            var go = new GameObject("TestSingleton");
            go.AddComponent<TestSingleton>();

            yield return null; // Wait for Awake

            Assert.IsNotNull(TestSingleton.Instance);
            Assert.AreEqual(go.GetComponent<TestSingleton>(), TestSingleton.Instance);
        }

        [UnityTest]
        public IEnumerator DuplicateInstance_IsDestroyed()
        {
            var go1 = new GameObject("TestSingleton1");
            var s1 = go1.AddComponent<TestSingleton>();

            yield return null; // Wait for Awake

            // Edit Mode tests do not always invoke dynamically added MonoBehaviour
            // Awake callbacks before the next object is created. Resolve the first
            // instance explicitly so the duplicate check is deterministic.
            Assert.AreSame(s1, TestSingleton.Instance);

            var go2 = new GameObject("TestSingleton2");
            var s2 = go2.AddComponent<TestSingleton>();

            yield return null; // Wait for second Awake

            Assert.IsNotNull(s1);
            Assert.AreEqual(s1, TestSingleton.Instance);
            Assert.IsTrue(s2 == null || s2 == TestSingleton.Instance); // should be destroyed
        }

        [UnityTest]
        public IEnumerator Instance_ThrowsIfNotFound()
        {
            LogAssert.ignoreFailingMessages = true;

            Assert.Throws<System.InvalidOperationException>(() =>
            {
                var unused = TestSingleton.Instance;
            });

            yield return null;
        }

        [UnityTest]
        public IEnumerator OnDestroy_ResetsSingletonInstance()
        {
            var go = new GameObject("TestSingleton");
            var s = go.AddComponent<TestSingleton>();

            yield return null;

            Assert.AreEqual(s, TestSingleton.Instance);

            if (Application.isPlaying)
            {
                Object.Destroy(go);
                yield return null; // Wait for OnDestroy
            }
            else
            {
                Object.DestroyImmediate(go);
            }

            // The singleton should no longer resolve to a live object. Do not inspect
            // the private backing field directly: destroyed UnityEngine.Object instances
            // can remain as fake-null wrappers even after the singleton has been reset.
            Assert.Throws<System.InvalidOperationException>(() =>
            {
                var unused = TestSingleton.Instance;
            });
        }

        [Test]
        public void PureSingleton_CreatesAndSharesInstance()
        {
            Assert.AreSame(TestPureSingleton.Instance, TestPureSingleton.Instance);
        }
    }
}
