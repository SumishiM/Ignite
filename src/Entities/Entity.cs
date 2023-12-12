using Ignite.Components;
using Ignite.src.Attributes;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Ignite.Entities
{
    public abstract class Entity
    {
        public event Action<Entity>? OnEntityActivated;
        public event Action<Entity>? OnEntityDeactivated;
        public event Action<int>? OnEntityDestroyed;


        public event Action<Entity, int>? OnComponentAdded;
        public event Action<Entity, int>? OnComponentRemoved;
        public event Action<Entity, int>? OnComponentModified;

        public int Id { get; init; }

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
        private bool _wasDeactivatedFromParent;


        private bool[] _availableComponents;
        /// <summary>
        /// List of every components on the entity
        /// </summary>
        private Dictionary<int, IComponent> _components;

        public ImmutableArray<IComponent> Components => _components
            .Where(kvp => _availableComponents[kvp.Key])
            .Select(kvp => kvp.Value)
            .ToImmutableArray();

        public ImmutableArray<int> ComponentIndices => _components
            .Where(kvp => _availableComponents[kvp.Key])
            .Select(kvp => kvp.Key)
            .ToImmutableArray();

        internal Entity(/*World,*/int id, IComponent[] components)
        {
            Id = id;

            InitializeComponents(components);
        }

        internal void InitializeComponents(IComponent[] components)
        {

        }

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


        public bool HasComponent<T>(T component) where T : IComponent
            => false;
        public bool HasComponent(Type type)
            => false;
        public bool HasComponent(int id)
            => false;

        public bool TryGetComponent<T>([NotNullWhen(true)] out T? component) where T : IComponent
        { component = default; return false; }
        public T? TryGetComponent<T>(T component) where T : IComponent
            => default(T);
        public IComponent? TryGetComponent(Type type)
            => null;
        public IComponent? TryGetComponent(int id)
            => null;

        public T GetComponent<T>() where T : IComponent
            => default(T)!;
        public IComponent GetComponent(Type type)
            => default!;

        public int GetComponentIndex<T>() where T : IComponent
            => 0;
        public int GetComponentIndex(Type type)
            => 0;

        public bool AddComponentOnce<T>(T component) where T : IComponent
            => false;
        public bool AddComponentOnce(Type type, IComponent component)
            => false;

        public Entity AddComponent<T>(T component) where T : IComponent
            => this;
        public Entity AddComponent(Type type, IComponent c)
            => this;

        public bool ReplaceComponent<T>(T component) where T : IComponent
            => false;
        public bool ReplaceComponent(Type type, IComponent component) 
            => false;

        public bool AddOrReplaceComponent<T>(T component) where T : IComponent
            => false;
        public bool AddOrReplaceComponent(Type type, IComponent component)
            => false;

        public bool RemoveComponent<T>() where T : IComponent
            => RemoveComponent(GetComponentIndex<T>());

        public bool RemoveComponent(Type type)
        {
            Debug.Assert(typeof(IComponent).IsAssignableFrom(type));
            return RemoveComponent(GetComponentIndex(type));
        }
        public bool RemoveComponent(int index)
            => false;
    }
}
