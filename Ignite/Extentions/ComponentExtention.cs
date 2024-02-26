using Ignite.Attributes;
using Ignite.Components;
using System.Reflection;

namespace Ignite.Extentions
{
    public static class ComponentExtention
    {
        /// <summary>
        /// Get a <see cref="IComponent"/> from the <see cref="RequireComponentAttribute"/> if the component has this attribute
        /// </summary>
        /// <param name="component"></param>
        /// <param name="type"></param>
        /// <returns>The component of <see cref="Type"/> <paramref name="type"/> or null</returns>
        public static IComponent? GetFromRequirement(this IComponent component, Type type)
        {
            if (component.GetType().GetCustomAttribute(typeof(RequireComponentAttribute)) is RequireComponentAttribute attribute)
            {
                if (attribute.Components.TryGetValue(type, out var requiredComponent))
                    return requiredComponent;
            }

            return null;
        }

        /// <summary>
        /// Get a <see cref="IComponent"/> of <see cref="Type"/> <typeparamref name="T"/> from the <see cref="RequireComponentAttribute"/> if the component has this attribute
        /// </summary>
        /// <param name="component"></param>
        /// <returns>The component of <see cref="Type"/> <typeparamref name="T"/> or null</returns>
        public static T? GetFromRequirement<T>(this IComponent component) where T : IComponent
            => (T?)component.GetFromRequirement(typeof(T));
        
    }
}
