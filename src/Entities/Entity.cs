using Ignite.Attributes;
using Ignite.Components;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;

namespace Ignite.Entities
{
    public abstract class Entity
    {
        /// <summary>
        /// Fired an event when the entity has been activated
        /// </summary>
        public event Action<Entity>? OnEntityActivated;

        /// <summary>
        /// Fired an event when the entity has been deactivated
        /// </summary>
        public event Action<Entity>? OnEntityDeactivated;

        /// <summary>
        /// Fired an event when the entity has been destroyed.
        /// With it's id
        /// </summary>
        public event Action<int>? OnEntityDestroyed;

        /// <summary>
        /// Fired an event whenever a component has been added.
        /// Arguments are the entity to which the component has been added and the component id.
        /// </summary>
        public event Action<Entity, int>? OnComponentAdded;

        /// <summary>
        /// Fired an event whenever a component has been removed.
        /// Arguments are the entity to which the component has been removed, 
        /// the component id and whether it was from a destroy.
        /// </summary>
        public event Action<Entity, int, bool>? OnComponentRemoved;

        /// <summary>
        /// Fired an event whenever a component has been modified.
        /// Arguments are the entity who's component has been modified and the component id.
        /// </summary>
        public event Action<Entity, int>? OnComponentModified;

        public int Id { get; init; }
        private Entity? _parent = null;

        /// <summary>
        /// Whether the entity is active or not.
        /// </summary>
        public bool IsActive => !IsDeactivated || !IsDestroyed;

        /// <summary>
        /// Whether the entity has been destroyed or not.
        /// </summary>
        public bool IsDestroyed { get; private set; }

        /// <summary>
        /// Whether the entity has been deactivated or not.
        /// </summary>
        public bool IsDeactivated { get; private set; }

        /// <summary>
        /// If we need to know when the entity has been deactivated by it's parent
        /// </summary>
        private bool _wasDeactivatedFromParent;

        /// <summary>
        /// If we need to know when the entity has been deactivated by it's parent
        /// </summary>
        public bool DeactivatedFromParent => _wasDeactivatedFromParent;

        /// <summary>
        /// List of available components for an index
        /// </summary>
        private bool[] _availableComponents;

        /// <summary>
        /// List of every components on the entity
        /// </summary>
        private Dictionary<int, IComponent> _components;

        private ComponentsLookupTable _lookup;

        /// <summary>
        /// List of the entity components
        /// </summary>
        public ImmutableArray<IComponent> Components => _components
            .Where(kvp => _availableComponents[kvp.Key])
            .Select(kvp => kvp.Value)
            .ToImmutableArray();

        /// <summary>
        /// List of the entity components indices
        /// </summary>
        public ImmutableArray<int> ComponentIndices => _components
            .Where(kvp => _availableComponents[kvp.Key])
            .Select(kvp => kvp.Key)
            .ToImmutableArray();

        internal Entity(/*World world,*/int id, IComponent[] components)
        {
            Id = id;

            _availableComponents = new bool[_lookup.TotalIndices];

            InitializeComponents(components);
        }

        internal void InitializeComponents(IComponent[] components)
        {
            _components = new Dictionary<int, IComponent>();

            foreach (IComponent component in components)
            {
                int index = _lookup.GetId(component.GetType());
                _components.TryAdd(index, component);
            }
        }

        /// <summary>
        /// Check whether the entity has all the required components when added in the world.
        /// </summary>
        internal void CheckForRequiredComponents()
        {
            ImmutableDictionary<int, Type> components = _components
                .Where(kvp => _availableComponents[kvp.Key])
                .ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value.GetType());

            foreach ((int _, Type type) in components)
            {

                RequireComponentAttribute? require = type
                    .GetCustomAttributes(typeof(RequireComponentAttribute), true)
                    .FirstOrDefault() as RequireComponentAttribute;

                if (require is null)
                    continue;

                foreach (Type requiredType in require.Types)
                {
                    int requiredId = requiredType.GetHashCode();

                    Debug.Assert(typeof(IComponent).IsAssignableFrom(requiredType),
                        $"The required type [{requiredType.Name}] is not a Component.");

                    Debug.Assert(!components.ContainsKey(requiredId),
                        $"Missing type [{requiredType}] required by {type.Name} in declaration!");
                }
            }
        }

        /// <summary>
        /// Check whether the entity has a component of type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="IComponent"/></typeparam>
        /// <param name="component"></param>
        /// <returns>Whether the entity has a component of type <typeparamref name="T"/>.</returns>
        public bool HasComponent<T>() where T : IComponent
            => HasComponent(GetComponentIndex<T>());

        /// <summary>
        /// Check whether the entity has a component of type <paramref name="type"/>
        /// </summary>
        /// <param name="type">Type of the component.</param>
        /// <returns>Whether the entity has a component of type <paramref name="type"/>.</returns>
        public bool HasComponent(Type type)
        {
            Debug.Assert(typeof(IComponent).IsAssignableFrom(type));
            return HasComponent(GetComponentIndex(type));
        }

        /// <summary>
        /// Check whether the entity has a component from it's <paramref name="index"/>
        /// </summary>
        /// <param name="index">Index of the component.</param>
        /// <returns>Whether the entity has a component.</returns>
        private bool HasComponent(int index)
            => index < _availableComponents.Length && _availableComponents[index];

        /// <summary>
        /// Try get a component of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Type of the component</typeparam>
        /// <param name="component">Component of type <typeparamref name="T"/>, 
        /// if there is no component of type <typeparamref name="T"/>, return null.</param>
        /// <returns>Whether the entity has a component of type <typeparamref name="T"/></returns>
        public bool TryGetComponent<T>([NotNullWhen(true)] out T? component) where T : IComponent
        {
            int index = GetComponentIndex(typeof(T));

            if (TryGetComponent(index, out IComponent? c))
            {
                component = (T)c;
                return true;
            }

            component = default;
            return false;
        }

        /// <summary>
        /// Try get a component at a given <paramref name="index"/>
        /// </summary>
        /// <param name="index">Index of the component.</param>
        /// <param name="component">Resulting component, 
        /// if there is no component with <paramref name="index"/>, return null.</param>
        /// <returns>Whether the entity has a component with the given <paramref name="index"/></returns>
        private bool TryGetComponent(int index, [NotNullWhen(true)] out IComponent? component)
        {
            if (HasComponent(index))
            {
                component = _components[index];
                return true;
            }

            component = default;
            return false;
        }

        /// <summary>
        /// Get a component of type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">type of the component.</typeparam>
        /// <returns>A component of type <typeparamref name="T"/></returns>
        public T GetComponent<T>() where T : IComponent
        {
            int index = GetComponentIndex<T>();
            Debug.Assert(HasComponent(index), $"The entity doesn't have a component of type '{typeof(T).Name}', maybe you should 'TryGetComponent'?");
            return (T)GetComponent(index);
        }

        /// <summary>
        /// Get a component of type <paramref name="type"/>
        /// </summary>
        /// <param name="type">Type of the component.</param>
        /// <returns>A component with instantiation type of <paramref name="type"/></returns>
        public IComponent GetComponent(Type type)
        {
            Debug.Assert(typeof(IComponent).IsAssignableFrom(type));

            int index = GetComponentIndex(type);
            Debug.Assert(HasComponent(index), $"The entity doesn't have a component of type '{type.Name}', maybe you should 'TryGetComponent'?");
            return GetComponent(index);
        }

        /// <summary>
        /// Get a component of type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">type of the component.</typeparam>
        /// <returns>A component of type <typeparamref name="T"/></returns>
        private IComponent GetComponent(int index)
        {
            Debug.Assert(HasComponent(index), $"The entity doesn't have a component of index '{index}', maybe you should 'TryGetComponent'?");
            return _components[index];
        }

        /// <summary>
        /// Get an index from a type <typeparamref name="T"/> component.
        /// </summary>
        /// <typeparam name="T">Type of the component we want the id.</typeparam>
        /// <returns>Index of a type <typeparamref name="T"/> component.</returns>
        private int GetComponentIndex<T>() where T : IComponent
            => GetComponentIndex(typeof(T));

        /// <summary>
        /// Get an index from a component of type <paramref name="type"/>.
        /// </summary>
        /// <param name="type">Type of the component.</param>
        /// <returns>Index of a component of type <paramref name="type"/>.</returns>
        private int GetComponentIndex(Type type)
        {
            Debug.Assert(typeof(IComponent).IsAssignableFrom(type));
            Debug.Assert(_lookup is not null, "The entity isn't set to the world!");
            return _lookup.GetId(type);
        }

        /// <summary>
        /// Add an empty component of type <typeparamref name="T"/> 
        /// if there isn't already one of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Type of the component.</typeparam>
        /// <returns>Whether the component has been added ot not.</returns>
        public bool AddComponentOnce<T>() where T : IComponent, new()
        {
            if (_lookup is null || HasComponent<T>())
            {
                Debug.Assert(_lookup is null);
                return false;
            }

            int index = GetComponentIndex<T>();

            T component = new();
            AddComponent(component, index);

            return true;
        }

        /// <summary>
        /// Add <paramref name="component"/> to the entity components 
        /// if there isn't already one of type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">Type of the component.</typeparam>
        /// <param name="component"></param>
        /// <returns>Whether the component has been added ot not.</returns>
        public bool AddComponentOnce<T>(T component) where T : IComponent
            => AddComponentOnce(typeof(T), component);

        /// <summary>
        /// Add <paramref name="component"/> to the entity components 
        /// if there isn't already one of type <paramref name="type"/>
        /// </summary>
        /// <param name="type">Type of the component.</param>
        /// <param name="component"></param>
        /// <returns>Whether the component has been added ot not.</returns>
        public bool AddComponentOnce(Type type, IComponent component)
        {
            Debug.Assert(typeof(IComponent).IsAssignableFrom(type));

            if (IsDestroyed || HasComponent(type))
                return false;

            return _components.TryAdd(GetComponentIndex(type), component);
        }

        /// <summary>
        /// Add a <paramref name="component"/> of type <typeparamref name="T"/> to the entity.
        /// </summary>
        /// <typeparam name="T">Type of the component.</typeparam>
        /// <param name="component">Component to add.</param>
        /// <returns>The entity with the added <paramref name="component"/>.</returns>
        public Entity AddComponent<T>(T component) where T : IComponent
        {
            if (_lookup is null)
            {
                // the world is null here so we just add the component
                _components.Add(_components.Count, component);
                return this;
            }

            int index = GetComponentIndex(typeof(T));
            AddComponent(component, index);

            return this;
        }

        /// <summary>
        /// Add a <paramref name="component"/> of type <paramref name="type"/> to the entity.
        /// </summary>
        /// <param name="type">Type of the component.</param>
        /// <param name="component">Component to add.</param>
        /// <returns>The entity with the added <paramref name="component"/></returns>
        public Entity AddComponent(Type type, IComponent component)
        {
            Debug.Assert(typeof(IComponent).IsAssignableFrom(type));
            AddComponent(component, GetComponentIndex(type));
            return this;
        }

        /// <summary>
        /// Add <paramref name="component"/> at <paramref name="index"/> to the entity.
        /// </summary>
        /// <param name="component">Component to add.</param>
        /// <param name="index">Index where to add the <paramref name="component"/></param>
        /// <returns>Whether the component has been added to the entity or not.</returns>
        private bool AddComponent(IComponent component, int index)
        {
            if (IsDestroyed)
            {
                Debug.Fail("The entity is already destroyed, cannot add a component to it.");
                return false;
            }

            if (HasComponent(index))
            {
                Debug.Fail("Trying to add a component on an entity where ther is already one of the same type. Try Replace(component) instead");
                return false;
            }

            //AddComponent_Internal(component, index);
            return false;
        }

        /// <summary>
        /// Replace a component of type <typeparamref name="T"/> with <paramref name="component"/>
        /// </summary>
        /// <typeparam name="T">Type of the component.</typeparam>
        /// <param name="component">Component to replace with.</param>
        /// <returns>Whether the component has been replaced or not.</returns>
        public bool ReplaceComponent<T>(T component) where T : IComponent
            => ReplaceComponent(typeof(T), component);

        /// <summary>
        /// Replace a component of type <paramref name="type"/> with <paramref name="component"/>
        /// </summary>
        /// <param name="type">Type of the component.</typeparam>
        /// <param name="component">Component to replace with.</param>
        /// <returns>Whether the component has been replaced or not.</returns>
        public bool ReplaceComponent(Type type, IComponent component)
        {
            Debug.Assert(typeof(IComponent).IsAssignableFrom(type));

            if (IsDestroyed)
                return false;

            if (!HasComponent(type))
            {
                Debug.Fail(
                    $"There is no component to replace with the type {type.Name}!" + 
                    $" Maybe use {nameof(AddOrReplaceComponent)}() instead.");
                return false;
            }

            // unsubscribe specialized components
            int index = GetComponentIndex(type);
            _components[index] = component;

            // todo : change this ?
            if( _parent is not null && component is IParentRelativeComponent
                && _parent.TryGetComponent(index, out IComponent? parentComponent))
            {
                //OnParentModified(index, parentComponent);
                return true;
            }

            //NotifyComponentReplaced(index, component);
            return true;
        }

        /// <summary>
        /// Add a <paramref name="component"/> of type <typeparamref name="T"/> to the entity, 
        /// if there is already a similar component, it replace it.
        /// </summary>
        /// <typeparam name="T">Type of the component.</typeparam>
        /// <param name="component">Component to add or replace with.</param>
        /// <returns>Whether the component as been added or replaced another component.</returns>
        public bool AddOrReplaceComponent<T>(T component) where T : IComponent
            => AddOrReplaceComponent(typeof(T), component);

        /// <summary>
        /// Add a <paramref name="component"/> of type <paramref name="type"/> to the entity, 
        /// if there is already a similar component, it replace it.
        /// </summary>
        /// <param name="type">Type of the component.</param>
        /// <param name="component">Component to add or replace with.</param>
        /// <returns>Whether the component as been added or replaced another component.</returns>
        public bool AddOrReplaceComponent(Type type, IComponent component)
        {
            if (HasComponent(type))
            {
                return ReplaceComponent(type, component);
            }

            return AddComponent(component, GetComponentIndex(type));
        }

        /// <summary>
        /// Remove a component of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Type of the component to remove.</typeparam>
        /// <returns>Whether the component has been removed or not.</returns>
        public bool RemoveComponent<T>() where T : IComponent
            => RemoveComponent(GetComponentIndex<T>());

        /// <summary>
        /// Remove a component of type <paramref name="type"/>.
        /// </summary>
        /// <param name="type">Type of the component to remove.</param>
        /// <returns>Whether the component has been removed or not.</returns>
        public bool RemoveComponent(Type type)
        {
            Debug.Assert(typeof(IComponent).IsAssignableFrom(type));
            return RemoveComponent(GetComponentIndex(type));
        }

        /// <summary>
        /// Remove a component of type <paramref name="index"/>.
        /// </summary>
        /// <param name="type">Type of the component to remove.</param>
        /// <returns>Whether the component has been removed or not.</returns>
        private bool RemoveComponent(int index)
        {
            if(!HasComponent(index))
            {
                Debug.Fail("There is no component to remove!");
                return false;
            }

            // unsubcribe events from components;

            _components[index] = default!;
            _availableComponents[index] = false;

            bool causeDestroy = _components.Count == 0 && !IsDeactivated;
            OnComponentRemoved?.Invoke(this, index, causeDestroy);
            //_parent?.UntrackComponent(index, OnParentModified);

            if(causeDestroy)
            {
                Destroy();
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Activate()
        {

        }

        public void Activate(Entity _)
        {
            if (!_wasDeactivatedFromParent)
                return;

            Activate();
        }

        public void Deactivate()
        {
        }

        public void Deactivate(Entity _)
        {
            if (IsDeactivated)
                return;

            _wasDeactivatedFromParent = true;
            Deactivate();
        }

        public void Destroy()
        {

        }
    }
}
