using Ignite.Components;
using System.Diagnostics;

namespace Ignite
{
    public partial class Node
    {
        public static Builder CreateBuilder(World world, string name = "Unnamed Node")
            => new(world, name);

        public sealed class Builder
        {
            private readonly World _world ;
            public string Name { get; set; } = "Unnamed Node";

            public Node? Parent { get; }
            private readonly List<Node> _children = [];
            private readonly List<Type> _componentsTypes = [];
            private readonly List<IComponent> _components = [];

            public Builder(World world, string name = "Unnamed Node", Node? parent = null)
            {
                _world = world;
                Name = name;
                Parent = parent ?? world.Root;
            }

            /// <summary>
            /// Add an empty <see cref="IComponent"/> of <see cref="Type"/> <typeparamref name="T"/> or add a given existing component.
            /// </summary>
            public Builder AddComponent<T>(ref T component) where T : struct, IComponent
                => AddComponent(typeof(T), component);

            /// <summary>
            /// Add an empty <see cref="IComponent"/> of <see cref="Type"/> <typeparamref name="T"/> or add a given existing component.
            /// </summary>
            public Builder AddComponent<T>(T? component = default) where T : IComponent
                => AddComponent(typeof(T), component);

            /// <summary>
            /// Add an empty <see cref="IComponent"/> of <see cref="Type"/> <paramref name="type"/> or add a given existing component.
            /// </summary>
            public Builder AddComponent(Type type, IComponent? component = null)
            {
                Debug.Assert(typeof(IComponent).IsAssignableFrom(type),
                    $"Why are we trying to add a component that isn't a IComponent ?");

                if (component == null)
                {
                    if (!_components.Any(c => c.GetType() == type))
                    {
                        if (Activator.CreateInstance(type, false) is IComponent c)
                            _components.Add(c);
                    }
                }
                else
                {
                    if (!_components.Any(c => c.GetType() == component.GetType()))
                        _components.Add(component);
                }
                return this;
            }

            /// <summary>
            /// Register a component to the builder
            /// </summary>
            public Builder AddComponents(params IComponent[] components)
            {
                foreach (var component in components)
                {
                    AddComponent(component);
                }

                return this;
            }

            /// <summary>
            /// Register an empty component to the builder
            /// </summary>
            public Builder AddComponents(params Type[] components)
            {
                foreach (var component in components)
                {
                    AddComponent(component, null);
                }

                return this;
            }

            public Node ToNode() => (Node)this;

            public static implicit operator Node(Builder b)
            {

                Node node = new(b._world);
                node.Name = b.Name;

                if (node.Id != 1)
                    b.Parent!.AddChild(node);

                node.AddChildren(b._children);

                for (int i = 0; i < b._components.Count; i++)
                {
                    IComponent component = b._components[i];
                    if (!node.HasComponent(component.GetType()))
                        node.AddComponent(ref component);
                }

                foreach (Type type in b._componentsTypes)
                {
                    if (!node.HasComponent(type))
                        node.AddComponent(type);
                }

                b._world.RegisterNode(node);

                return node;
            }
        }
    }
}
