using UnityEngine;

namespace MLGWorks.Utils.DependencyInjection
{
    /// <summary>
    /// Interface defining a contract for objects that require explicit initialization
    /// after dependency injection has been performed.
    /// </summary>
    public interface IInitializable
    {
        /// <summary>
        /// Performs initialization logic that depends on injected services or other setup.
        /// This method is called after all dependencies have been injected.
        /// </summary>
        void Initialize();
    }
}
