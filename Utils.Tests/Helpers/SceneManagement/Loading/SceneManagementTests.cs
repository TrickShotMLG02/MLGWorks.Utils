using MLGWorks.Utils.Helpers.SceneManagement.Core;
using MLGWorks.Utils.Helpers.SceneManagement.Loading;
using MLGWorks.Utils.Helpers.SceneManagement.Transitions;
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

        [Test]
        public void SceneReference_DefaultNameCanBeOverridden()
        {
            sceneReference.name = "Loading";

            Assert.AreEqual("Loading (unassigned)", sceneReference.ToString());
        }

        [Test]
        public void SceneReference_DisplayNameFallsBackToSceneName()
        {
            Assert.AreEqual(string.Empty, sceneReference.DisplayName);
        }

        [Test]
        public void SceneReference_NotesDefaultToEmpty()
        {
            Assert.IsNotNull(sceneReference.Notes);
            Assert.IsEmpty(sceneReference.Notes);
        }

        [Test]
        public void SceneLoader_CanLoadReturnsFalseForNull()
        {
            Assert.IsFalse(SceneLoader.CanLoad(null));
        }

        [Test]
        public void SceneLoader_CanLoadReturnsFalseForUnassignedReference()
        {
            Assert.IsFalse(SceneLoader.CanLoad(sceneReference));
        }

        [Test]
        public void SceneLoader_IsLoadedReturnsFalseForUnassignedReference()
        {
            Assert.IsFalse(SceneLoader.IsLoaded(sceneReference));
        }

        [Test]
        public void SceneLoader_GetLoadedSceneRejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() => SceneLoader.GetLoadedScene(null));
        }

        [Test]
        public void SceneLoader_GetLoadedSceneRejectsUnassignedReference()
        {
            Assert.Throws<InvalidOperationException>(() => SceneLoader.GetLoadedScene(sceneReference));
        }

        [Test]
        public void SceneLoader_UnloadRejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() => SceneLoader.Unload(null));
        }

        [Test]
        public void SceneLoader_UnloadRejectsUnassignedReference()
        {
            Assert.Throws<InvalidOperationException>(() => SceneLoader.Unload(sceneReference));
        }

        [Test]
        public void SceneLoader_UnloadAsyncRejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() => SceneLoader.UnloadAsync(null));
        }

        [Test]
        public void SceneLoader_UnloadAsyncRejectsUnassignedReference()
        {
            Assert.Throws<InvalidOperationException>(() => SceneLoader.UnloadAsync(sceneReference));
        }

        [Test]
        public void SceneLoader_LoadAsyncSingleRejectsUnassignedReference()
        {
            Assert.Throws<InvalidOperationException>(() => SceneLoader.LoadAsync(sceneReference));
        }

        [Test]
        public void SceneLoader_LoadAsyncAdditiveRejectsUnassignedReference()
        {
            Assert.Throws<InvalidOperationException>(() => SceneLoader.LoadAsync(
                sceneReference,
                UnityEngine.SceneManagement.LoadSceneMode.Additive));
        }

        [Test]
        public void SceneLoader_LoadWithTransitionRejectsNullScene()
        {
            Assert.Throws<ArgumentNullException>(() => SceneLoader.LoadWithTransition(null, null));
        }

        [Test]
        public void SceneLoader_LoadWithTransitionRejectsNullTransition()
        {
            Assert.Throws<ArgumentNullException>(() => SceneLoader.LoadWithTransition(sceneReference, null));
        }

        [Test]
        public void SceneLoadCoordinator_StartsWithNoActiveLoads()
        {
            using (var coordinator = new SceneLoadCoordinator())
            {
                Assert.AreEqual(0, coordinator.ActiveLoadCount);
                Assert.IsFalse(coordinator.IsLoading(null));
                Assert.IsFalse(coordinator.IsLoading(sceneReference));
            }
        }

        [Test]
        public void SceneLoadCoordinator_RejectsNullScene()
        {
            using (var coordinator = new SceneLoadCoordinator())
            {
                Assert.Throws<ArgumentNullException>(() => coordinator.LoadAsync(null));
            }
        }

        [Test]
        public void SceneLoadCoordinator_ThrowPolicyRejectsInvalidScene()
        {
            using (var coordinator = new SceneLoadCoordinator(DuplicateSceneLoadBehavior.Throw))
            {
                Assert.Throws<InvalidOperationException>(() => coordinator.LoadAsync(sceneReference));
            }
        }

        [Test]
        public void SceneLoadCoordinator_DisposeIsIdempotent()
        {
            var coordinator = new SceneLoadCoordinator();

            Assert.DoesNotThrow(() => coordinator.Dispose());
            Assert.DoesNotThrow(() => coordinator.Dispose());
        }

        [Test]
        public void SceneLoadCoordinator_DisposedLoadThrowsObjectDisposedException()
        {
            var coordinator = new SceneLoadCoordinator();
            coordinator.Dispose();

            Assert.Throws<ObjectDisposedException>(() => coordinator.LoadAsync(sceneReference));
        }

        [Test]
        public void SceneLoadCoordinator_DisposedUnloadThrowsObjectDisposedException()
        {
            var coordinator = new SceneLoadCoordinator();
            coordinator.Dispose();

            Assert.Throws<ObjectDisposedException>(() => coordinator.UnloadAsync(sceneReference));
        }

        [Test]
        public void SceneLoadCoordinator_DisposedTickThrowsObjectDisposedException()
        {
            var coordinator = new SceneLoadCoordinator();
            coordinator.Dispose();

            Assert.Throws<ObjectDisposedException>(() => coordinator.Tick());
        }

        [Test]
        public void SceneLoader_LoadWithTransitionRejectsInvalidSceneBeforeTransitionExecution()
        {
            var transition = new ImmediateTransition();

            Assert.Throws<InvalidOperationException>(() => SceneLoader.LoadWithTransition(sceneReference, transition));
            Assert.IsFalse(transition.PlayOutCalled);
        }

        private sealed class ImmediateTransition : ISceneTransition
        {
            public bool PlayOutCalled { get; private set; }

            public void PlayOut(SceneReference scene, Action onCompleted)
            {
                PlayOutCalled = true;
                onCompleted?.Invoke();
            }

            public void PlayIn(SceneReference scene, Action onCompleted)
            {
                onCompleted?.Invoke();
            }
        }
    }
}
