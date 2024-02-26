using Ignite.Attributes;
using Ignite.Components;
using System.Reflection;

namespace Ignite.Extentions
{
    public static class ComponentExtention
    {
        public static IComponent? GetFromRequirement(this IComponent component, Type type)
        {
            if (component.GetType().GetCustomAttribute(typeof(RequireComponentAttribute)) is RequireComponentAttribute attribute)
            {
                if (attribute.Components.TryGetValue(type, out var requiredComponent))
                    return requiredComponent;
            }

            return null;
        }
    }
}
