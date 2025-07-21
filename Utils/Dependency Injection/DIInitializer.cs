using MLGWorks.Utils.DependencyInjection.Attributes;
using System;
using System.Linq;
using UnityEngine;

namespace MLGWorks.Utils.DependencyInjection
{
    /// <summary>
    /// MonoBehaviour responsible for automatic dependency injection and initialization
    /// of all MonoBehaviour instances in the scene.<br />
    ///<br />
    /// This initializer performs three key tasks during its Awake phase:<br />
    /// 1. Automatically registers all MonoBehaviours decorated with <see cref="DIServiceAttribute"/>
    ///    into the <see cref="ServiceLocator"/> using their concrete types.<br />
    /// 2. Injects dependencies into all MonoBehaviours by setting fields marked with <see cref="InjectAttribute"/>.<br />
    /// 3. Calls the <see cref="IInitializable.Initialize"/> method on all MonoBehaviours implementing the interface,
    ///    allowing them to run custom initialization logic after injection.<br />
    ///<br />
    /// The script execution order is set very high (10000) to ensure this runs after
    /// nearly all other Awake methods, allowing all scene objects to be created and ready.
    /// </summary>
    [DefaultExecutionOrder(10000)]  // Run this *after* almost everything else
    public class DIInitializer : MonoBehaviour
    {
        /// <summary>
        /// Called by Unity during the Awake lifecycle phase.
        /// Performs service auto-registration, dependency injection, and post-injection initialization.
        /// </summary>
        private void Awake()
        {
            // Retrieve all MonoBehaviour instances in the scene, including inactive ones.
            var monos = FindObjectsOfType<MonoBehaviour>(true);

            // Automatically register all MonoBehaviours decorated with [DIServiceAttribute].
            foreach (var mono in monos)
            {
                var type = mono.GetType();
                if (Attribute.IsDefined(type, typeof(DIServiceAttribute)))
                {
                    // Register this instance as a service using its concrete type.
                    ServiceLocator.Register(type, mono);
                }
            }

            // Inject dependencies into all MonoBehaviours by setting [Inject] fields.
            foreach (var mono in monos)
                Injector.Inject(mono);

            // Invoke Initialize() on all MonoBehaviours that implement IInitializable,
            // allowing for any additional setup after injection.
            foreach (var init in monos.OfType<IInitializable>())
                init.Initialize();
        }
    }
}
