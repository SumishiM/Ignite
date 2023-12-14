using Ignite.Attributes;
using Ignite.Components;
using Ignite.Utils;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

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
        public event Action<UniqueID>? OnEntityDestroyed;

        /// <summary>
        /// Fired an event whenever a component has been added.
        /// Arguments are the entity to which the component has been added and the component id.
        /// </summary>
        public event Action<Entity, TypeUniqueID>? OnComponentAdded;

        /// <summary>
        /// Fired an event whenever a component has been removed.
        /// Arguments are the entity to which the component has been removed, 
        /// the component id and whether it was from a destroy.
        /// </summary>
        public event Action<Entity, TypeUniqueID, bool>? OnComponentRemoved;

        /// <summary>
        /// Fired an event whenever a component has been modified.
        /// Arguments are the entity who's component has been modified and the component id.
        /// </summary>
        public event Action<Entity, TypeUniqueID>? OnComponentModified;

        public UniqueID Id { get; init; }
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
        /// List of every components on the entity
        /// </summary>
        private Dictionary<TypeUniqueID, IComponent> _components;

        private ComponentsLookupTable _lookup;

        /// <summary>
        /// List of the entity components
        /// </summary>
        public ImmutableArray<IComponent> Components => _components
            .Select(kvp => kvp.Value)
            .ToImmutableArray();

        /// <summary>
        /// List of the entity components indices
        /// </summary>
        public ImmutableArray<TypeUniqueID> ComponentIndices => _components
            .Select(kvp => kvp.Key)
            .ToImmutableArray();

        internal Entity(World world, UniqueID id, IComponent[] components)
        {
            Id = id;
            _lookup = world.Lookup;

            InitializeComponents(components);
        }

        internal void InitializeComponents(IComponent[] components)
        {
            _components = new Dictionary<TypeUniqueID, IComponent>();

            foreach (IComponent component in components)
            {
                TypeUniqueID index = _lookup.GetId(component.GetType());
                _components.TryAdd(index, component);
            }
        }

        /// <summary>
        /// Check whether the entity has all the required components when added in the world.
        /// </summary>
        internal void CheckForRequiredComponents()
        {
            ImmutableDictionary<TypeUniqueID, Type> components = _components
                .ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value.GetType());

            foreach ((TypeUniqueID _, Type type) in components)
            {

                RequireComponentAttribute? require = type
                    .GetCustomAttributes(typeof(RequireComponentAttribute), true)
                    .FirstOrDefault() as RequireComponentAttribute;

                if (require is null)
                    continue;

                foreach (Type requiredType in require.Types)
                {
                    TypeUniqueID requiredId = TypeUniqueID.GetOrCreateUniqueID(requiredType);

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
            => HasComponent(typeof(T));

        /// <summary>
        /// Check whether the entity has a component of type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="IComponent"/></typeparam>
        /// <param name="component"></param>
        /// <returns>Whether the entity has a component of type <typeparamref name="T"/>.</returns>
        public bool HasComponent(TypeUniqueID index)
            => _components.ContainsKey(index);

        /// <summary>
        /// Check whether the entity has a component of type <paramref name="type"/>
        /// </summary>
        /// <param name="type">Type of the component.</param>
        /// <returns>Whether the entity has a component of type <paramref name="type"/>.</returns>
        public bool HasComponent(Type type)
        {
            Debug.Assert(typeof(IComponent).IsAssignableFrom(type) && TypeUniqueID.Exist(type));
            return _components.ContainsKey(TypeUniqueID.GetOrCreateUniqueID(type));
        }

        /// <summary>
        /// Try get a component of <see cref="Type"/> <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Type of the component</typeparam>
        /// <param name="component">Component of <see cref="Type"/> <typeparamref name="T"/>, 
        /// if there is no component of <see cref="Type"/> <typeparamref name="T"/>, return null.</param>
        /// <returns>Whether the entity has a component of <see cref="Type"/> <typeparamref name="T"/></returns>
        public bool TryGetComponent<T>([NotNullWhen(true)] out T? component) where T : IComponent
        {
            if (TryGetComponent(typeof(T), out IComponent? c))
            {
                component = (T)c!;
                return true;
            }

            component = default;
            return false;
        }

        /// <summary>
        /// Try get a component at a given <see cref="Type"/>
        /// </summary>
        /// <param name="type"><see cref="Type"/> of the component.</param>
        /// <param name="component">Resulting component, 
        /// if there is no component with this <see cref="Type"/>, return null.</param>
        /// <returns>Whether the entity has a component with the given <see cref="Type"/></returns>
        private bool TryGetComponent(Type type, [NotNullWhen(true)] out IComponent? component)
        {
            if (HasComponent(type))
            {
                component = _components[TypeUniqueID.GetOrCreateUniqueID(type)];
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
            Debug.Assert(typeof(IComponent).IsAssignableFrom(typeof(T)) && TypeUniqueID.Exist(typeof(T)), 
                $"The entity doesn't have a component of type '{typeof(T).Name}', maybe you should 'TryGetComponent'?");
            return (T)_components[TypeUniqueID.Get<T>()];
        }

        /// <summary>
        /// Get a component of type <paramref name="type"/>
        /// </summary>
        /// <param name="type">Type of the component.</param>
        /// <returns>A component with instantiation type of <paramref name="type"/></returns>
        public IComponent GetComponent(Type type)
        {
            Debug.Assert(typeof(IComponent).IsAssignableFrom(type) && TypeUniqueID.Exist(type), 
                $"The entity doesn't have a component of type '{type.Name}', maybe you should 'TryGetComponent'?");
            return _components[TypeUniqueID.Get(type)];
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

            TypeUniqueID index = TypeUniqueID.Get<T>();

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

            return _components.TryAdd(TypeUniqueID.Get(type), component);
        }

        /// <summary>
        /// Add a <paramref name="component"/> of type <typeparamref name="T"/> to the entity.
        /// </summary>
        /// <typeparam name="T">Type of the component.</typeparam>
        /// <param name="component">Component to add.</param>
        /// <returns>The entity with the added <paramref name="component"/>.</returns>
        public Entity AddComponent<T>(T component) where T : IComponent
        {

            TypeUniqueID index = TypeUniqueID.Get<T>();
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
            AddComponent(component, TypeUniqueID.Get(type));
            return this;
        }

        /// <summary>
        /// Add <paramref name="component"/> at <paramref name="index"/> to the entity.
        /// </summary>
        /// <param name="component">Component to add.</param>
        /// <param name="index">Index where to add the <paramref name="component"/></param>
        /// <returns>Whether the component has been added to the entity or not.</returns>
        private bool AddComponent(IComponent component, TypeUniqueID index)
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
            TypeUniqueID index = TypeUniqueID.Get(type);
            _components[index] = component;

            // todo : change this ?
            if( _parent is not null && component is IParentRelativeComponent
                && _parent.TryGetComponent(type, out IComponent? parentComponent))
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

            return AddComponent(component, TypeUniqueID.Get(type));
        }

        /// <summary>
        /// Remove a component of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Type of the component to remove.</typeparam>
        /// <returns>Whether the component has been removed or not.</returns>
        public bool RemoveComponent<T>() where T : IComponent
            => RemoveComponent(TypeUniqueID.Get<T>());

        /// <summary>
        /// Remove a component of type <paramref name="type"/>.
        /// </summary>
        /// <param name="type">Type of the component to remove.</param>
        /// <returns>Whether the component has been removed or not.</returns>
        public bool RemoveComponent(Type type)
        {
            Debug.Assert(typeof(IComponent).IsAssignableFrom(type));
            return RemoveComponent(TypeUniqueID.Get(type));
        }

        /// <summary>
        /// Remove a component of type <paramref name="index"/>.
        /// </summary>
        /// <param name="type">Type of the component to remove.</param>
        /// <returns>Whether the component has been removed or not.</returns>
        private bool RemoveComponent(TypeUniqueID index)
        {
            if(!HasComponent(index))
            {
                Debug.Fail("There is no component to remove!");
                return false;
            }

            // unsubcribe events from components;

            _components.Remove(index);

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
