﻿using Ignite.Components;
using System.Diagnostics;

namespace Ignite
{
    public partial class Node
    {
        public static Builder CreateBuilder(World world, string name = "Unnamed Node")
            => new(world, name);

        public sealed class Builder
        {
            private readonly World _world;
            public World World => _world;
            public string Name { get; set; } = "Unnamed Node";

            public Node? Parent { get; set; }
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
            public Builder AddComponent<T>(T? component = default) where T : IComponent
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

            public Builder AddComponents(params IComponent[] components)
            {
                foreach (var component in components)
                {
                    AddComponent(component);
                }

                return this;
            }

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

                foreach (IComponent component in b._components)
                {
                    node.AddComponent(component);
                }

                foreach (Type type in b._componentsTypes)
                {
                    node.AddComponent(type);
                }

                b._world.RegisterNode(node);

                return node;
            }
        }
    }
}
