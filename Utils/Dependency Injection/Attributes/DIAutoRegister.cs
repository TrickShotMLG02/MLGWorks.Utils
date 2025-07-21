using System;

namespace MLGWorks.Utils.DependencyInjection.Attributes
{
    /// <summary>
    /// Marks a class as a Dependency Injection service that should be
    /// automatically registered in the service locator during initialization.
    ///
    /// Classes decorated with this attribute are typically instantiated and
    /// registered by the DI system without manual registration.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DIServiceAttribute : Attribute
    {
        // Intentionally left empty; acts as a marker attribute.
        // You may extend this attribute later with parameters
        // such as service lifetime (singleton, transient, etc.).
    }
}
