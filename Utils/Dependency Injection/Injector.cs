using MLGWorks.Utils.DependencyInjection.Attributes;
using System;
using System.Reflection;
using UnityEngine;

namespace MLGWorks.Utils.DependencyInjection
{
    /// <summary>
    /// Provides functionality to inject dependencies into objects by setting
    /// fields marked with the <see cref="InjectAttribute"/> using services
    /// registered in the <see cref="ServiceLocator"/>.
    /// </summary>
    public static class Injector
    {
        /// <summary>
        /// Injects registered services into the specified target object's fields
        /// marked with the <see cref="InjectAttribute"/>.
        /// </summary>
        /// <param name="target">
        /// The object instance into which dependencies will be injected.
        /// If null, the method returns immediately without doing anything.
        /// </param>
        public static void Inject(object target)
        {
            if (target == null)
            {
                return;
            }

            Type type = target.GetType();

            // Walk the inheritance chain so private injectable fields declared by a
            // base class are not skipped when a derived type is injected.
            for (Type currentType = type; currentType != null; currentType = currentType.BaseType)
            {
                FieldInfo[] fields = currentType.GetFields(
                    BindingFlags.Instance |
                    BindingFlags.NonPublic |
                    BindingFlags.Public |
                    BindingFlags.DeclaredOnly);

                foreach (FieldInfo field in fields)
                {
                    // Check if the field is decorated with [Inject]
                    if (Attribute.IsDefined(field, typeof(InjectAttribute)))
                    {
                        // Try to get a registered service matching the field's type
                        if (ServiceLocator.TryGet(field.FieldType, out var service))
                        {
                            // Inject the service instance into the field
                            field.SetValue(target, service);
                        }
                        else
                        {
                            // Log a warning if no service is registered for the field's type
                            Debug.LogWarning(
                                $"[Injector] No service registered for type {field.FieldType.Name} on {type.Name}");
                        }
                    }
                }
            }
        }
    }
}
