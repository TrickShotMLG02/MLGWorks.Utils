using MLGWorks.Utils.Helpers.SceneManagement.Core;
using MLGWorks.Utils.Helpers.SceneManagement.Loading;
using NUnit.Framework;
using System;
using UnityEngine;

namespace MLGWorks.Utils.Tests.Helpers.SceneManagement.Loading
{
    [TestFixture]
    public class SceneManagementTests
    {
        private SceneReference sceneReference;

        [SetUp]
        public void SetUp()
        {
            sceneReference = ScriptableObject.CreateInstance<SceneReference>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(sceneReference);
        }

        [Test]
        public void NewReference_IsInvalidAndHasSafeMetadataDefaults()
        {
            Assert.IsFalse(sceneReference.IsValid);
            Assert.IsFalse(sceneReference.IsInBuildSettings);
            Assert.IsFalse(SceneLoader.CanLoad(sceneReference));
            Assert.AreEqual(string.Empty, sceneReference.ScenePath);
            Assert.AreEqual(string.Empty, sceneReference.SceneName);
            Assert.AreEqual(string.Empty, sceneReference.DisplayName);
            Assert.AreEqual(string.Empty, sceneReference.Notes);
        }

        [Test]
        public void ToString_UsesAssetNameForUnassignedReference()
        {
            sceneReference.name = "Menu";

            Assert.AreEqual("Menu (unassigned)", sceneReference.ToString());
        }

        [Test]
        public void SceneLoader_RejectsNullAndInvalidReferences()
        {
            Assert.Throws<ArgumentNullException>(() => SceneLoader.Load(null));
            Assert.Throws<InvalidOperationException>(() => SceneLoader.Load(sceneReference));
            Assert.IsFalse(SceneLoader.IsLoaded(null));
            Assert.IsFalse(SceneLoader.IsLoaded(sceneReference));
        }

        [Test]
        public void SceneLoadCoordinator_RejectsInvalidAndDisposedUsage()
        {
            var coordinator = new SceneLoadCoordinator();
            Assert.Throws<InvalidOperationException>(() => coordinator.LoadAsync(sceneReference));

            coordinator.Dispose();
            Assert.Throws<ObjectDisposedException>(() => coordinator.Tick());
        }
    }
}
