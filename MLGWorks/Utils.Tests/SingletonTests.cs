using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MLGWorks.Utility.Tests
{
    public class SingletonTests
    {
        private class TestSingleton : Singleton<TestSingleton>
        { }

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

            Object.Destroy(go);
            yield return null; // Wait for OnDestroy

            // _instance should now be null
            var field = typeof(Singleton<TestSingleton>).GetField("_instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var value = field.GetValue(null);

            Assert.IsNull(value);
        }
    }
}
