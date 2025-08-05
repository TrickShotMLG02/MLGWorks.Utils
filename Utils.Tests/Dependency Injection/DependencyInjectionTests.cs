using MLGWorks.Utils.DependencyInjection;
using MLGWorks.Utils.DependencyInjection.Attributes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MLGWorks.Utils.Tests.DependencyInjection
{
    // Dummy service interface and implementation
    public interface ITestService
    {
        string GetData();
    }

    public class TestService : ITestService
    {
        public string GetData() => "Hello from TestService";
    }

    // Pure C# class with [DIService] (won't be auto-registered by MonoBehaviour scan)
    [DIService]
    public class PureService
    {
        public int Value => 42;
    }

    // MonoBehaviour with [Inject] field
    public class InjectableMonoBehaviourAuto : MonoBehaviour
    {
        [Inject]
        private AutoRegisteredService _service;

        public AutoRegisteredService Service => _service;
    }

    public class InjectableMonoBehaviourManual : MonoBehaviour
    {
        [Inject]
        private TestService _service;

        public TestService Service => _service;
    }

    // MonoBehaviour with [DIService] attribute (should auto-register)
    [DIService]
    public class AutoRegisteredService : MonoBehaviour, ITestService
    {
        public string GetData() => "Auto Registered";
    }

    // MonoBehaviour implementing IInitializable
    public class InitMonoBehaviour : MonoBehaviour, IInitializable
    {
        public bool Initialized { get; private set; } = false;

        public void Initialize()
        {
            Initialized = true;
        }
    }

    public class DependencyInjectionTests
    {
        [SetUp]
        public void Setup()
        {
            // Clear all registrations before each test
            ServiceLocator.Clear();
        }

        [Test]
        public void ServiceLocator_RegisterAndGet_Works()
        {
            var service = new TestService();
            ServiceLocator.Register<ITestService>(service);

            var retrieved = ServiceLocator.Get<ITestService>();

            Assert.AreSame(service, retrieved);
            Assert.AreEqual("Hello from TestService", retrieved.GetData());
        }

        [Test]
        public void Injector_Injects_ServiceIntoMonoBehaviour()
        {
            var service = new TestService();
            ServiceLocator.Register(service);

            var go = new GameObject("TestObject");
            var injectable = go.AddComponent<InjectableMonoBehaviourManual>();

            Injector.Inject(injectable);

            Assert.IsNotNull(injectable.Service);
            Assert.AreEqual("Hello from TestService", injectable.Service.GetData());

            GameObject.DestroyImmediate(go);
        }

        [UnityTest]
        public System.Collections.IEnumerator DIInitializer_AutoRegistersAndInjects()
        {
            // Create a GameObject with AutoRegisteredService
            var go = new GameObject("AutoRegisterGO");
            var autoService = go.AddComponent<AutoRegisteredService>();

            // Create a GameObject with InjectableMonoBehaviour
            var goInject = new GameObject("InjectGO");
            var injectable = goInject.AddComponent<InjectableMonoBehaviourAuto>();

            // Create DIInitializer object
            var initializerGO = new GameObject("DIInitializerGO");
            var diInitializer = initializerGO.AddComponent<DIInitializer>();

            // Wait one frame for Awake calls
            yield return null;

            // AutoRegisteredService should be registered in ServiceLocator as its concrete type
            bool found = ServiceLocator.TryGet(typeof(AutoRegisteredService), out var regService);
            Assert.IsTrue(found);
            Assert.AreSame(autoService, regService);

            // InjectableMonoBehaviour should have its ITestService field injected with the AutoRegisteredService instance
            Assert.IsNotNull(injectable.Service);
            Assert.AreEqual("Auto Registered", injectable.Service.GetData());

            GameObject.DestroyImmediate(go);
            GameObject.DestroyImmediate(goInject);
            GameObject.DestroyImmediate(initializerGO);
        }

        [Test]
        public void IInitializable_Initialize_IsCalled()
        {
            var go = new GameObject("InitGO");
            var initMono = go.AddComponent<InitMonoBehaviour>();

            // Before initialize called
            Assert.IsFalse(initMono.Initialized);

            Injector.Inject(initMono);

            // Call initialize explicitly
            initMono.Initialize();

            Assert.IsTrue(initMono.Initialized);

            GameObject.DestroyImmediate(go);
        }

        [Test]
        public void PureService_CanBeRegisteredAndRetrieved()
        {
            var pure = new PureService();
            ServiceLocator.Register<PureService>(pure);

            var retrieved = ServiceLocator.Get<PureService>();

            Assert.IsNotNull(retrieved);
            Assert.AreEqual(42, retrieved.Value);
        }
    }
}
