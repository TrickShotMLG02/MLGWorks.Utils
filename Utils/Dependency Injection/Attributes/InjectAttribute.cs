using System;

namespace MLGWorks.Utils.DependencyInjection.Attributes
{
    /// <summary>
    /// Marks a field to be injected by the dependency injection system.
    /// Fields decorated with this attribute will have their values set
    /// automatically by the <see cref="Injector"/> using the <see cref="ServiceLocator"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class InjectAttribute : Attribute
    {
        // Intentionally left empty; acts as a marker attribute.
    }
}
