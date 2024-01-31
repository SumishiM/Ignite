using Ignite.Components;

namespace Ignite
{
    /**
     * This part of the Node class handle reflection related stuffs
     **/
    public partial class Node
    {
        private void AddRequiredComponents(IComponent component)
        {
            Type type = component.GetType();
            if (_lookup.RequiredComponentsLookup.TryGetValue(_lookup[type], out var requirements))
            {
                foreach (var requirement in requirements)
                {
                    AddComponent(requirement);
                }
            }
        }
    }
}
