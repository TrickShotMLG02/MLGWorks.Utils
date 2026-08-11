using MLGWorks.Utils.Helpers.Unity;
using NUnit.Framework;
using System;
using UnityEngine;

namespace MLGWorks.Utils.Tests.Helpers.Unity
{
    public class UnityObjectExtensionsTests
    {
        private sealed class TestComponent : MonoBehaviour
        {
        }

        [Test]
        public void GetOrAddComponent_ReusesExistingAndAddsMissingComponents()
        {
            var gameObject = new GameObject("ComponentExtensionsTest");

            TestComponent first = gameObject.GetOrAddComponent<TestComponent>();
            TestComponent second = gameObject.GetOrAddComponent<TestComponent>();

            Assert.AreSame(first, second);
            Assert.AreEqual(1, gameObject.GetComponents<TestComponent>().Length);
            gameObject.SafeDestroy();
        }

        [Test]
        public void SetActiveIfChanged_OnlyChangesWhenNecessary()
        {
            var gameObject = new GameObject("ActiveExtensionsTest");
            gameObject.SetActive(false);

            Assert.IsFalse(gameObject.SetActiveIfChanged(false));
            Assert.IsTrue(gameObject.SetActiveIfChanged(true));
            Assert.IsFalse(gameObject.SetActiveIfChanged(true));
            Assert.IsTrue(gameObject.activeSelf);

            gameObject.SafeDestroy();
        }

        [Test]
        public void SetParentIfChanged_OnlyChangesWhenNecessary()
        {
            var child = new GameObject("Child");
            var parent = new GameObject("Parent").transform;

            Assert.IsTrue(child.transform.SetParentIfChanged(parent));
            Assert.IsFalse(child.transform.SetParentIfChanged(parent));
            Assert.AreSame(parent, child.transform.parent);

            child.SafeDestroy();
            parent.gameObject.SafeDestroy();
        }

        [Test]
        public void FindInChildren_FindsInactiveDescendants()
        {
            var root = new GameObject("SearchRoot");
            var child = new GameObject("SearchChild");
            child.transform.SetParent(root.transform);
            child.SetActive(false);
            TestComponent expected = child.AddComponent<TestComponent>();

            Assert.AreSame(expected, root.FindInChildren<TestComponent>());
            Assert.IsNull(root.FindInChildren<AudioSource>());

            root.SafeDestroy();
        }

        [Test]
        public void ComponentHierarchyExtensions_FindParentsAndAddToRootWhenMissing()
        {
            var root = new GameObject("ComponentRoot");
            var child = new GameObject("ComponentChild");
            child.transform.SetParent(root.transform);
            TestComponent expected = child.AddComponent<TestComponent>();

            Assert.IsTrue(root.TryGetComponentInChildren(out TestComponent found));
            Assert.AreSame(expected, found);
            Assert.IsTrue(child.TryGetComponentInParent(out TestComponent parentFound));
            Assert.AreSame(expected, parentFound);

            AudioSource added = root.GetOrAddComponentInChildren<AudioSource>();
            Assert.AreSame(root, added.gameObject);
            Assert.AreSame(added, root.GetOrAddComponentInChildren<AudioSource>());

            root.SafeDestroy();
        }

        [Test]
        public void ResetLocalTransform_OnlyWritesWhenValuesDiffer()
        {
            var gameObject = new GameObject("TransformResetTest");
            Assert.IsFalse(gameObject.transform.ResetLocalTransform());

            gameObject.transform.localPosition = new Vector3(1f, 2f, 3f);
            gameObject.transform.localRotation = Quaternion.Euler(0f, 30f, 0f);
            gameObject.transform.localScale = Vector3.one * 2f;

            Assert.IsTrue(gameObject.transform.ResetLocalTransform());
            Assert.AreEqual(Vector3.zero, gameObject.transform.localPosition);
            Assert.AreEqual(Quaternion.identity, gameObject.transform.localRotation);
            Assert.AreEqual(Vector3.one, gameObject.transform.localScale);

            gameObject.SafeDestroy();
        }

        [Test]
        public void RecursiveExtensions_UpdateOnlyChangedHierarchyValues()
        {
            var root = new GameObject("RecursiveRoot");
            var child = new GameObject("RecursiveChild");
            child.transform.SetParent(root.transform);

            Assert.AreEqual(2, root.SetLayerRecursively(7));
            Assert.AreEqual(0, root.SetLayerRecursively(7));
            Assert.AreEqual(7, root.layer);
            Assert.AreEqual(7, child.layer);

            Assert.AreEqual(2, root.SetActiveHierarchy(false));
            Assert.AreEqual(0, root.SetActiveHierarchy(false));
            Assert.IsFalse(root.activeSelf);
            Assert.IsFalse(child.activeSelf);

            root.SafeDestroy();
        }

        [Test]
        public void HierarchyExtensions_FindPathsAndDestroyChildren()
        {
            var root = new GameObject("HierarchyRoot");
            var branch = new GameObject("Branch");
            branch.transform.SetParent(root.transform);
            var leaf = new GameObject("Leaf");
            leaf.transform.SetParent(branch.transform);
            leaf.SetActive(false);

            Assert.AreSame(leaf.transform, root.transform.FindChildPath("Branch/Leaf"));
            Assert.IsNull(root.transform.FindChildPath("Branch/Leaf", includeInactive: false));
            Assert.AreEqual(1, root.transform.DestroyChildren(immediate: true));
            Assert.AreEqual(0, root.transform.childCount);

            root.SafeDestroy();
        }

        [Test]
        public void UIExtensions_OnlyUpdateChangedValues()
        {
            var gameObject = new GameObject("UIExtensionsTest");
            RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
            CanvasGroup canvasGroup = gameObject.AddComponent<CanvasGroup>();

            Assert.IsFalse(rectTransform.SetAnchoredPositionIfChanged(Vector2.zero));
            Assert.IsTrue(rectTransform.SetAnchoredPositionIfChanged(new Vector2(10f, 20f)));
            Assert.IsFalse(rectTransform.SetAnchoredPositionIfChanged(new Vector2(10f, 20f)));
            Assert.IsTrue(rectTransform.SetSizeDeltaIfChanged(new Vector2(100f, 50f)));
            Assert.IsFalse(rectTransform.SetSizeDeltaIfChanged(new Vector2(100f, 50f)));

            Assert.IsFalse(canvasGroup.SetVisibleIfChanged(true));
            Assert.IsTrue(canvasGroup.SetVisibleIfChanged(false));
            Assert.IsFalse(canvasGroup.SetVisibleIfChanged(false));
            Assert.IsFalse(canvasGroup.interactable);
            Assert.IsFalse(canvasGroup.blocksRaycasts);

            gameObject.SafeDestroy();
        }

        [Test]
        public void NullExtensionTargets_ThrowOrNoOpAsDocumented()
        {
            GameObject gameObject = null;
            Transform transform = null;

            Assert.Throws<ArgumentNullException>(() => gameObject.GetOrAddComponent<TestComponent>());
            Assert.Throws<ArgumentNullException>(() => gameObject.SetActiveIfChanged(true));
            Assert.Throws<ArgumentNullException>(() => transform.SetParentIfChanged(null));
            Assert.Throws<ArgumentNullException>(() => gameObject.FindInChildren<TestComponent>());

            UnityEngine.Object unityObject = null;
            Assert.DoesNotThrow(() => unityObject.SafeDestroy());
        }
    }
}
