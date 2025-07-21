using UnityEngine;

namespace MLGWorks.Utils.DependencyInjection
{
    /// <summary>
    /// Bootstrapper MonoBehaviour responsible for registering core services
    /// early in the application's lifecycle.<br />
    ///<br />
    /// This class is intended to register:<br />
    /// - Pure C# service classes (non-MonoBehaviour).<br />
    /// - MonoBehaviour services that do <b>not</b> have the <see cref="DIServiceAttribute"/> attached.<br />
    ///<br />
    /// MonoBehaviour services decorated with <see cref="DIServiceAttribute"/> are
    /// automatically registered by the <see cref="DIInitializer"/>, so they should not
    /// be registered here to avoid duplicates.<br />
    ///<br />
    /// Executes in a very early script execution order to ensure services
    /// are registered before any dependency injection occurs.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class ServiceBootstrapper : MonoBehaviour
    {
        /// <summary>
        /// Called by Unity during the Awake phase.
        /// Use this method to register all core services with the <see cref="ServiceLocator"/>.
        ///
        /// Register pure C# services directly here, and register MonoBehaviour services only if
        /// they do not have the <see cref="DIServiceAttribute"/>.
        /// </summary>
        protected void Awake()
        {
            // Register all your core services here
            // Example:
            // ServiceLocator.Register<Logger>(new Logger());
            // ServiceLocator.Register<GameSettings>(new GameSettings());
        }
    }
}
