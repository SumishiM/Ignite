using Ignite.Components;
using Ignite.Systems;

namespace Ignite
{
    public partial class World
    {
        /// <summary>
        /// Create an empty <see cref="World.Builder"/> for a <see cref="World"/>
        /// </summary>
        public static Builder CreateBuilder() => new();

        public class Builder : IDisposable
        {
            private readonly HashSet<Type> _systemTypes = [];
            private readonly List<(string, Type[])> _defaultEmptyNodes = [];
            private readonly List<(string, IComponent[])> _defaultNodes = [];

            internal Builder() { }

            /// <summary>
            /// Add a <see cref="ISystem"/> of <see cref="Type"/> <typeparamref name="T"/> to the world.
            /// </summary>
            public Builder AddSystem<T>()
            {
                return AddSystem(typeof(T));
            }

            /// <summary>
            /// Add a <see cref="ISystem"/> <see cref="Type"/> to the world.
            /// </summary>
            public Builder AddSystem(Type system)
            {
                _systemTypes.Add(system);
                return this;
            }

            /// <summary>
            /// Add an array of <see cref="ISystem"/> <see cref="Type"/> to the world.
            /// </summary>
            public Builder AddSystems(params Type[] systems)
            {
                foreach (var system in systems)
                {
                    _systemTypes.Add(system);
                }
                return this;
            }

            /// <summary>
            /// Add an empty node to the world
            /// </summary>
            public Builder AddNode(string name = "Unnamed Node")
            {
                _defaultEmptyNodes.Add((name, []));
                return this;
            }

            /// <summary>
            /// Add a node with empty components to the world
            /// </summary>
            public Builder AddNode(string name = "Unnamed Node", params Type[] types)
            {
                _defaultEmptyNodes.Add((name, types));
                return this;
            }

            /// <summary>
            /// Add a node with some components to the world
            /// </summary>
            public Builder AddNode(string name = "Unnamed Node", params IComponent[] components)
            {
                _defaultNodes.Add((name, components));
                return this;
            }

            /// <summary>
            /// Create the world from the data stored in the builder
            /// </summary>
            public World Build()
            {
                List<ISystem> systems = [];
                foreach (var type in _systemTypes)
                {
                    if (Activator.CreateInstance(type) is ISystem system)
                    {
                        systems.Add(system);
                    }
                    else
                    {
                        throw new Exception($"{type.FullName} isn't a ISystem.");
                    }
                }

                World world = new(systems);

                // register default nodes
                foreach (var (name, types) in _defaultEmptyNodes)
                {
                    world.AddNode(name, types);
                }

                foreach (var (name, components) in _defaultNodes)
                {
                    world.AddNode(name, components);
                }

                return world;
            }

            public void Dispose()
            {
                _systemTypes.Clear();
                _defaultEmptyNodes.Clear();
                _defaultNodes.Clear();
            }
        }
    }
}

