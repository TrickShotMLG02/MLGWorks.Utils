using MLGWorks.Utils.Helpers.Pooling.Core;
using MLGWorks.Utils.Helpers.Pooling.Unity;
using NUnit.Framework;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace MLGWorks.Utils.Tests.Helpers.Pooling
{
    public class ObjectPoolTests
    {
        private sealed class Item
        {
            public int Id;
        }

        private sealed class PoolableItem : IPoolable
        {
            public int Acquired;
            public int Released;

            public void OnPoolAcquire() => Acquired++;

            public void OnPoolRelease() => Released++;
        }

        private sealed class PooledComponent : MonoBehaviour, IPoolable
        {
            public int Acquired;
            public int Released;

            public void OnPoolAcquire() => Acquired++;

            public void OnPoolRelease() => Released++;
        }

        [Test]
        public void GetRelease_ReusesTheSameObject()
        {
            int created = 0;
            var pool = new ObjectPool<Item>(() => new Item { Id = ++created });

            Item first = pool.Get();
            pool.Release(first);
            Item second = pool.Get();

            Assert.AreSame(first, second);
            Assert.AreEqual(1, created);
            Assert.AreEqual(1, pool.CountActive);
            Assert.AreEqual(0, pool.CountInactive);

            pool.Dispose();
        }

        [Test]
        public void InitialCapacity_PrecreatesObjects()
        {
            int created = 0;
            using var pool = new ObjectPool<Item>(() => new Item { Id = ++created }, initialCapacity: 3);

            Assert.AreEqual(3, created);
            Assert.AreEqual(3, pool.CountInactive);
            Assert.AreEqual(3, pool.CountAll);
        }

        [Test]
        public void Prewarm_AddsOnlyInactiveObjects()
        {
            int created = 0;
            using var pool = new ObjectPool<Item>(() => new Item { Id = ++created });
            Item active = pool.Get();

            pool.Prewarm(2);

            Assert.AreEqual(3, created);
            Assert.AreEqual(1, pool.CountActive);
            Assert.AreEqual(2, pool.CountInactive);
        }

        [Test]
        public void PrewarmStep_RespectsMaximumPerStepAndCapacity()
        {
            int created = 0;
            using var pool = new ObjectPool<Item>(() => new Item { Id = ++created }, maxCapacity: 5);

            Assert.AreEqual(2, pool.PrewarmStep(2));
            Assert.AreEqual(2, pool.PrewarmStep(2));
            Assert.AreEqual(1, pool.PrewarmStep(2));
            Assert.AreEqual(0, pool.PrewarmStep(2));
            Assert.AreEqual(5, created);
            Assert.AreEqual(5, pool.CountInactive);
        }

        [Test]
        public void BatchOperations_AddAndReleaseRequestedItems()
        {
            int created = 0;
            using var pool = new ObjectPool<Item>(() => new Item { Id = ++created });
            var items = new List<Item>();

            pool.GetMany(items, 3);
            Assert.AreEqual(3, items.Count);
            Assert.AreEqual(3, pool.CountActive);

            pool.ReleaseMany(items);
            Assert.AreEqual(0, pool.CountActive);
            Assert.AreEqual(3, pool.CountInactive);
        }

        [Test]
        public void PoolableCallbacks_RunOnAcquireAndRelease()
        {
            using var pool = new ObjectPool<PoolableItem>(() => new PoolableItem());
            PoolableItem item = pool.Get();
            pool.Release(item);
            pool.Get();

            Assert.AreEqual(2, item.Acquired);
            Assert.AreEqual(1, item.Released);
        }

        [Test]
        public void DuplicateRelease_Throws()
        {
            using var pool = new ObjectPool<Item>(() => new Item());
            Item item = pool.Get();
            pool.Release(item);

            Assert.Throws<InvalidOperationException>(() => pool.Release(item));
        }

        [Test]
        public void MaxCapacity_DestroysExcessReleasedObjects()
        {
            int destroyed = 0;
            using var pool = new ObjectPool<Item>(
                () => new Item(),
                onDestroy: _ => destroyed++,
                maxCapacity: 1);

            Item first = pool.Get();
            Item second = pool.Get();
            pool.Release(first);
            pool.Release(second);

            Assert.AreEqual(1, pool.CountInactive);
            Assert.AreEqual(1, pool.CountAll);
            Assert.AreEqual(1, destroyed);
        }

        [Test]
        public void Dispose_DestroysActiveAndInactiveObjects()
        {
            int destroyed = 0;
            var pool = new ObjectPool<Item>(() => new Item(), onDestroy: _ => destroyed++);
            Item inactive = pool.Get();
            pool.Release(inactive);
            Item active = pool.Get();
            Item secondActive = pool.Get();

            pool.Dispose();

            Assert.AreEqual(2, destroyed);
            Assert.Throws<ObjectDisposedException>(() => pool.Get());
        }

        [Test]
        public void GameObjectPool_ReusesAndParentsInstances()
        {
            var prefab = new GameObject("PoolPrefab");
            var parent = new GameObject("PoolParent").transform;
            var pool = new GameObjectPool(prefab, parent, initialCapacity: 1);

            GameObject instance = pool.Get();
            Assert.IsTrue(instance.activeSelf);
            Assert.AreSame(parent, instance.transform.parent);

            pool.Release(instance);
            Assert.IsFalse(instance.activeSelf);

            GameObject reused = pool.Get();
            Assert.AreSame(instance, reused);

            pool.Dispose();
            UnityEngine.Object.DestroyImmediate(prefab);
            UnityEngine.Object.DestroyImmediate(parent.gameObject);
        }

        [Test]
        public void ComponentPool_InvokesComponentResetHooks()
        {
            var prefab = new GameObject("ComponentPoolPrefab");
            prefab.AddComponent<PooledComponent>();
            using var pool = new ComponentPool<PooledComponent>(prefab);

            PooledComponent component = pool.Get();
            pool.Release(component);
            pool.Get();

            Assert.AreEqual(2, component.Acquired);
            Assert.AreEqual(1, component.Released);

            UnityEngine.Object.DestroyImmediate(prefab);
        }

        [Test]
        public void SpecializedPools_RejectPrefabsWithoutRequiredComponents()
        {
            var prefab = new GameObject("InvalidPoolPrefab");

            Assert.Throws<ArgumentException>(() => new ParticleSystemPool(prefab));
            Assert.Throws<ArgumentException>(() => new AudioSourcePool(prefab));

            UnityEngine.Object.DestroyImmediate(prefab);
        }

        [Test]
        public void Constructor_InvalidCapacities_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => new ObjectPool<Item>(null));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ObjectPool<Item>(() => new Item(), initialCapacity: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ObjectPool<Item>(() => new Item(), maxCapacity: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ObjectPool<Item>(() => new Item(), maxCapacity: -2));
            Assert.Throws<ArgumentException>(() =>
                new ObjectPool<Item>(() => new Item(), initialCapacity: 2, maxCapacity: 1));
        }

        [Test]
        public void Release_FromAnotherPool_ThrowsWithoutChangingOwnership()
        {
            using var firstPool = new ObjectPool<Item>(() => new Item());
            using var secondPool = new ObjectPool<Item>(() => new Item());
            Item item = firstPool.Get();

            Assert.Throws<InvalidOperationException>(() => secondPool.Release(item));
            Assert.AreEqual(1, firstPool.CountActive);
            Assert.AreEqual(0, secondPool.CountActive);

            firstPool.Release(item);
        }

        [Test]
        public void Operations_AfterDispose_ThrowAndDisposeIsIdempotent()
        {
            int destroyed = 0;
            var pool = new ObjectPool<Item>(() => new Item(), onDestroy: _ => destroyed++);
            pool.Dispose();
            pool.Dispose();

            Assert.AreEqual(0, destroyed);
            Assert.Throws<ObjectDisposedException>(() => pool.Get());
            Assert.Throws<ObjectDisposedException>(() => pool.Clear());
            Assert.Throws<ObjectDisposedException>(() => pool.Prewarm(1));
            Assert.Throws<ObjectDisposedException>(() => pool.ReleaseMany(new List<Item>()));
        }
    }
}
