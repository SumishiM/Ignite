using Ignite.Components;
using System.Diagnostics;

namespace Ignite
{
    public partial class Node
    {
        public static Builder CreateBuilder(World world)
            => new (world);

        public sealed class Builder
        {
            private readonly World _world;

            public Node? Parent { get; set; }
            private readonly List<Node> _children = [];
            private readonly List<Type> _componentsTypes = [];
            private readonly List<IComponent> _components = [];

            public Builder(World world)
            {
                _world = world;
            }

            /// <summary>
            /// Add an empty <see cref="IComponent"/> of <see cref="Type"/> <typeparamref name="T"/> or add a given existing component.
            /// </summary>
            public Builder AddComponent<T>(T? component = null) where T : class, IComponent, new()
                => AddComponent(typeof(T), component);

            /// <summary>
            /// Add an empty <see cref="IComponent"/> of <see cref="Type"/> <paramref name="type"/> or add a given existing component.
            /// </summary>
            public Builder AddComponent(Type type, IComponent? component = null) 
            {
                Debug.Assert(typeof(IComponent).IsAssignableFrom(type), 
                    $"Whay are we trying to add a component that isn't a IComponent ?");

                if (component == null)
                {
                    if (_componentsTypes.Contains(type) && !_components.Any(c => c.GetType() == type))
                        _componentsTypes.Add(type);
                }
                else
                {
                    if (!_components.Any(c => c.GetType() == component.GetType()))
                        _components.Add(component);
                }
                return this;
            }

            public Node ToNode() => this;

            public static implicit operator Node(Builder b)
            {
                Node node = new(b._world);
                node.SetParent(b.Parent);
                node.AddChildren(b._children);

                foreach (IComponent component in b._components)
                {
                    node.AddComponent(component);
                }

                foreach (Type type in b._componentsTypes)
                {
                    node.AddComponent(type);
                }

                node.Id = UID.Next();
                b._world.RegisterNode(node);

                return node;
            }
        }
    }
}
