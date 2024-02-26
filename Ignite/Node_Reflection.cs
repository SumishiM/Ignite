using Ignite.Attributes;
using Ignite.Components;
using System.Collections.Immutable;
using System.Reflection;

namespace Ignite
{
    /**
     * This part of the Node class handle reflection related stuffs
     **/
    public partial class Node
    {
        private void AddRequiredComponents(IComponent component)
        {
            var requireComponents = component.GetType().GetCustomAttribute<RequireComponentAttribute>();

            if (requireComponents == null)
                return;

            Type type = component.GetType();
            var builder = ImmutableDictionary.CreateBuilder<Type, IComponent>();

            if (_lookup.RequiredComponentsLookup.TryGetValue(_lookup[type], out var requirements))
            {
                foreach (var requirement in requirements)
                {
                    AddComponent(requirement);
                    builder.Add(requirement, GetComponent(requirement));
                }
            }

            requireComponents.Components = builder.ToImmutableDictionary();
        }
    }
}
