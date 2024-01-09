using Ignite.Components;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;

namespace Ignite
{
    /** 
     * This part of the Node class is responsible of the components management
     * 
    **/
    public partial class Node
    {
        /// <summary>
        /// Trigger when a component is added, send the component as payload
        /// </summary>
        public event Action<Node, int>? OnComponentAdded;

        /// <summary>
        /// Trigger when a component is replaced, send the old component id and the component as payload
        /// </summary>
        public event Action<Node, int>? OnComponentReplaced;
        public event Action<Node, int>? OnComponentModified;

        /// <summary>
        /// Trigger when a component is removed, send the component id and the whether it was from node deletion or not as payload
        /// </summary>
        public event Action<Node, int, bool>? OnComponentRemoved;

        /// <summary>
        /// Collection of components referenced by there id
        /// </summary>
        public Dictionary<int, IComponent> Components { get; protected set; } =
            new Dictionary<int, IComponent>();

        public HashSet<int> ComponentsIndices => Components.Keys.ToHashSet();

        private readonly ComponentLookupTable _lookup;

        public Node ( World world, params IComponent[] components )
            : this(world)
        {
            foreach ( var component in components )
            {
                Components.Add(_lookup[component.GetType()], component);
            }
        }

        /// <summary>
        /// Check whether the node has a component of <see cref="Type"/> <typeparamref name="T"/> or not
        /// </summary>
        public bool HasComponent<T> () where T : class, IComponent
            => HasComponent(_lookup[typeof(T)]);

        /// <summary>
        /// Check whether the node has a component of <see cref="Type"/> <paramref name="type"/> or not
        /// </summary>
        public bool HasComponent ( Type type )
            => HasComponent(_lookup[type]);

        /// <summary>
        /// Check whether the node has a component from it's index or not
        /// </summary>
        internal bool HasComponent (int index )
        {
            return Components.ContainsKey(index);
        }

        /// <summary>
        /// Check whether the node has a component or not
        /// </summary>
        internal bool HasComponent ( IComponent component )
        {
            return Components.ContainsKey(_lookup[component]);
        }

        /// <summary>
        /// Try to get a component of <see cref="Type"/> <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T"><see cref="Type"/> of the component we want to get</typeparam>
        /// <param name="component">Output component</param>
        /// <returns>Whether the component has been got or not.</returns>
        public bool TryGetComponent<T> ( [NotNullWhen(true)] out T? component )
            where T : class, IComponent
        {
            if ( TryGetComponent(typeof(T), out IComponent? result) )
            {
                component = (T)result;
                return true;
            }

            component = null;
            return false;
        }

        /// <summary>
        /// Try to get a component of <see cref="Type"/> <paramref name="type"/>
        /// </summary>
        /// <param name="type"><see cref="Type"/> of the component we want to get</param>
        /// <param name="component">Output component</param>
        /// <returns>Whether the component has been got or not.</returns>
        public bool TryGetComponent ( Type type, [NotNullWhen(true)] out IComponent? component )
        {
            Debug.Assert(typeof(IComponent).IsAssignableFrom(type),
                $"Why are we trying to get and object that isn't a component!?");

            if ( Components.TryGetValue(_lookup[type], out IComponent? value) )
            {
                component = value;
                return true;
            }

            component = null;
            return false;
        }

        /// <summary>
        /// Get a component of <see cref="Type"/> <typeparamref name="T"/>
        /// </summary>
        public T GetComponent<T> () where T : class, IComponent
        {
            Debug.Assert(HasComponent<T>(), $"This node doesn't contain this component");
            return (T)Components[_lookup[typeof(T)]];
        }

        /// <summary>
        /// Get a component of <see cref="Type"/> <paramref name="type"/>
        /// </summary>
        public IComponent GetComponent ( Type type )
        {
            Debug.Assert(typeof(IComponent).IsAssignableFrom(type),
                $"Why are we trying to get and object that isn't a component!?");
            Debug.Assert(HasComponent(type), $"This node doesn't contain this component");

            return Components[_lookup[type]];
        }

        /// <summary>
        /// Get the index of a component of <see cref="Type"/> <typeparamref name="T"/>
        /// </summary>
        internal int GetComponentIndex<T> ()
        {
            return _lookup[typeof(T)];
        }

        /// <summary>
        /// Get the index of a component of <see cref="Type"/> <paramref name="type"/>
        /// </summary>
        internal int GetComponentIndex ( Type type )
        {
            return _lookup[type];
        }

        /// <summary>
        /// Add an empty <see cref="IComponent"/> of <see cref="Type"/> <typeparamref name="T"/>
        /// </summary>
        public Node AddComponent<T> () where T : class, IComponent, new()
            => AddComponent(new T());

        /// <summary>
        /// Add an empty <see cref="IComponent"/> of <see cref="Type"/> <paramref name="type"/>
        /// </summary>
        public Node AddComponent ( Type type )
        {
            Debug.Assert(typeof(IComponent).IsAssignableFrom(type),
                $"Why are we trying to add/replace a component with a type that isn't a component ?");

            return AddComponent((IComponent)Activator.CreateInstance(type)!);
        }

        /// <summary>
        /// Add a <paramref name="component"/> of <see cref="Type"/> <typeparamref name="T"/>
        /// </summary>
        public Node AddComponent<T> ( T component ) where T : class, IComponent
        {
            Debug.Assert(!HasComponent(component),
                $"Why are we adding a component to a node already having one of the same type ? " +
                $"Try AddOrReplaceComponent instead.");

            Components[_lookup[component.GetType()]] = component;
            component.Node = this;
            OnComponentAdded?.Invoke(this, _lookup[component]);
            return this;
        }

        /// <summary>
        /// Add an empty <see cref="IComponent"/> of <see cref="Type"/> <typeparamref name="T"/>
        /// or replace the already set component of the same <see cref="Type"/>
        /// </summary>
        public Node AddOrReplaceComponent<T> () where T : class, IComponent, new()
            => AddOrReplaceComponent(new T());

        /// <summary>
        /// Add an empty <see cref="IComponent"/> of <see cref="Type"/> <paramref name="type"/>
        /// or replace the already set component of the same <see cref="Type"/>
        /// </summary>
        public Node AddOrReplaceComponent ( Type type )
        {
            Debug.Assert(typeof(IComponent).IsAssignableFrom(type),
                $"Why are we trying to add/replace a component with a type that isn't a component ?");

            return AddOrReplaceComponent((IComponent)Activator.CreateInstance(type)!);
        }

        /// <summary>
        /// Add a <paramref name="component"/> of <see cref="Type"/> <typeparamref name="T"/>
        /// or replace the already set component of the same <see cref="Type"/>
        /// </summary>
        public Node AddOrReplaceComponent<T>(T component) where T : class, IComponent
        {
            int index = _lookup[component.GetType()];
            if (Components.ContainsKey(index))
            {
                Components[index] = component;
                component.Node = this;

                //OnComponentReplaced?.Invoke(this, index, component);

                return this; 
            }

            Components[_lookup[component.GetType()]] = component;
            component.Node = this;

            OnComponentAdded?.Invoke(this, _lookup[component]);

            return this;
        }

        /// <summary>
        /// Remove a of <see cref="Type"/> <typeparamref name="T"/> if there is one on this node
        /// </summary>
        public Node RemoveComponent<T> ()
        {
            int index = _lookup[typeof(T)];
            Components.Remove(index);
            OnComponentRemoved?.Invoke(this, index, false);
            return this;
        }

        /// <summary>
        /// Remove a of <see cref="Type"/> <paramref name="type"/> if there is one on this node
        /// </summary>
        public Node RemoveComponent ( Type type )
        {
            int index = _lookup[type];
            Components.Remove(index);
            OnComponentRemoved?.Invoke(this, index, false);
            return this;
        }

        private void RemoveAllComponents()
        {
            foreach ( var component in Components )
            {
                Components.Remove(component.Key);
                OnComponentRemoved?.Invoke(this,component.Key, true);
            }
        }
    }
}
