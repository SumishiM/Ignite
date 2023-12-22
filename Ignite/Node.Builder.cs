using Ignite.Components;
using Ignite.Utils;

namespace Ignite
{
    public partial class Node
    {
        public static Builder CreateBuilder(World world)
            => new (world);

        public sealed class Builder
        {
            private readonly World _world;

            private Node? _parent;
            private readonly List<Node> _children = [];
            private readonly List<Type> _componentsTypes = [];
            private readonly List<IComponent> _components = [];

            public Builder(World world)
            {
                _world = world;
            }

            public Node? Parent { get => _parent; set => _parent = value; }

            /// <summary>
            /// Add an empty <see cref="IComponent"/> of <see cref="Type"/> <typeparamref name="T"/> or add a given existing component.
            /// </summary>
            public Builder AddComponent<T>(T? component = null) where T : class, IComponent, new()
            {
                if (component == null)
                {
                    if (_componentsTypes.Contains(typeof(T)) && !_components.Any(c => c.GetType() == typeof(T)))
                        _componentsTypes.Add(typeof(T));
                }
                else
                {
                    if (!_components.Any(c => c.GetType() == component.GetType()))
                        _components.Add(component);
                }
                return this;
            }

            /// <summary>
            /// Add a child node if it's not already added or contained in a already listed child
            /// </summary>
            public Node AddChild(Node child)
            {
                // Todo : algo to check of any children contains this child or not
                if (!_children.Contains(child))
                    _children.Add(child);
                return this;
            }

            /// <summary>
            /// Add a list of node if they're not already added or contained in a already listed child
            /// </summary>
            public Node AddChildren(IEnumerable<Node> children)
            {
                foreach (Node child in children)
                    AddChild(child);
                return this;
            }

            public Node ToNode()
                => this;

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
                node.Id = NodeId.Next(0);
                b._world.RegisterNode(node);

                return node;
            }
        }
    }
}
