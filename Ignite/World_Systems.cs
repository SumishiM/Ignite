using Ignite.Systems;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Ignite
{
    public partial class World
    {

        /// <summary>
        /// Systems executed on world start
        /// </summary>
        private readonly SortedList<int, (IStartSystem system, int context)> _cachedStartSystems;

        /// <summary>
        /// Systems executed on world exit
        /// </summary>
        private readonly SortedList<int, (IExitSystem system, int context)> _cachedExitSystem;

        /// <summary>
        /// Systems executed on world update
        /// </summary>
        private readonly SortedList<int, (IUpdateSystem system, int context)> _cachedUpdateSystems;

        /// <summary>
        /// Systems executed on world render
        /// </summary>
        private readonly SortedList<int, (IRenderSystem system, int context)> _cachedRenderSystems;

        /// <summary>
        /// System infos for each registered system.
        /// </summary>
        private readonly Dictionary<int, SystemInfo> _systems;

        private readonly ImmutableDictionary<int, ISystem> _idToSystems;

        private readonly ImmutableDictionary<Type, int> _typeToSystem;


        private readonly ImmutableHashSet<int> _pauseSystems;

        private readonly ImmutableHashSet<int> _ignorePauseSystems;


        /// <summary>
        /// Ids of <see cref="IStartSystem"/> initialized.
        /// </summary>
        private readonly HashSet<int> _systemsInitialized;

        private readonly Dictionary<int, bool> _pendingToggleSystems;


        private readonly Dictionary<int, Context> _contexts;


        /// <summary>
        /// Enable or disable every systems waiting to be toggled
        /// </summary>
        private void TogglePendingSystems()
        {
            ImmutableDictionary<int, bool> pendingToggle = _pendingToggleSystems.ToImmutableDictionary();
            _pendingToggleSystems.Clear();

            foreach (var (id, isEnabled) in pendingToggle)
            {
                if (isEnabled)
                    DisableSystem(id);
                else
                    EnableSystem(id);
            }
        }

        /// <summary>
        /// Enable a <see cref="ISystem"/> of <see cref="Type"/> <typeparamref name="T"/>
        /// </summary>
        /// <param name="immediate">Whether the system should be enabled immediatly</param>
        public bool EnableSystem<T>(bool immediate = false) where T : ISystem
            => EnableSystem(typeof(T), immediate);

        /// <summary>
        /// Enable a <see cref="ISystem"/> of given <see cref="Type"/>
        /// </summary>
        /// <param name="immediate">Whether the system should be enabled immediatly</param>
        public bool EnableSystem(Type type, bool immediate = false)
        {
            Debug.Assert(typeof(ISystem).IsAssignableFrom(type), 
                $"Why are we trying to enable a system from a type that isn't ?");
            
            if (!_typeToSystem.TryGetValue(type, out int id))
            {
                return false;
            }
            
            return EnableSystem(id, immediate);
        }

        /// <summary>
        /// Enable a <see cref="ISystem"/> from it's id
        /// </summary>
        /// <param name="immediate">Whether the system should be enabled immediatly</param>
        private bool EnableSystem(int id, bool immediate = false)
        {
            // Check if the system can be enabled
            if (_pendingToggleSystems.TryGetValue(id, out var enabled))
            {
                if (enabled)
                    return false;
            }
            if (_systems[id].IsActive)
            {
                return false;
            }
            if (!immediate)
            {
                _pendingToggleSystems[id] = true;
                return true;
            }

            // update info
            _systems[id] = _systems[id] with { IsActive = true };
            CacheSystem(id);

            return true;
        }

        /// <summary>
        /// Disable a <see cref="ISystem"/> of <see cref="Type"/> <typeparamref name="T"/>
        /// </summary>
        /// <param name="immediate">Whether the system should be disabled immediatly</param>
        public bool DisableSystem<T>(bool immediate = false) where T : ISystem
            => DisableSystem(typeof(T), immediate);

        /// <summary>
        /// Disable a <see cref="ISystem"/> of given <see cref="Type"/> 
        /// </summary>
        /// <param name="immediate">Whether the system should be disabled immediatly</param>
        public bool DisableSystem(Type type, bool immediate = false)
        {
            Debug.Assert(typeof(ISystem).IsAssignableFrom(type),
                $"Why are we trying to disable a system from a type that isn't ?");

            if (!_typeToSystem.TryGetValue(type, out int id))
            {
                return false;
            }

            return DisableSystem(id, immediate);
        }

        /// <summary>
        /// Disable a <see cref="ISystem"/> from it's id
        /// </summary>
        /// <param name="immediate">Whether the system should be disabled immediatly</param>
        private bool DisableSystem(int id, bool immediate = false)
        {
            // Check if the system can be enabled
            if (_pendingToggleSystems.TryGetValue(id, out var enabled))
            {
                if (enabled)
                    return false;
            }
            if (_systems[id].IsActive)
            {
                return false;
            }
            if (!immediate)
            {
                _pendingToggleSystems[id] = false;
                return true;
            }

            // update info
            _systems[id] = _systems[id] with { IsActive = false };
            UncacheSystem(id);

            return true;
        }

        /// <summary>
        /// Add a system to the cache
        /// </summary>
        private void CacheSystem(int id)
        {
            ISystem system = _idToSystems[id];
            int contextId = _systems[id].ContextId;

            // Cache system
            if (system is IStartSystem startSystem)
            {
                _cachedStartSystems.Add(id, (startSystem, contextId));

                // Initialize system if not
                if (!_systemsInitialized.Contains(id))
                {
                    Context context = _contexts[id];
                    startSystem.Start(context);
                    _systemsInitialized.Add(id);
                }
            }

            if (system is IUpdateSystem updateSystem) _cachedUpdateSystems.Add(id, (updateSystem, contextId));
            if (system is IRenderSystem renderSystem) _cachedRenderSystems.Add(id, (renderSystem, contextId));
        }

        /// <summary>
        /// Remove a system from cache
        /// </summary>
        private void UncacheSystem(int id)
        {
            ISystem system = _idToSystems[(int)id];
            if (system is IStartSystem) _cachedStartSystems.Remove(id);
            if (system is IUpdateSystem) _cachedUpdateSystems.Remove(id);
            if (system is IRenderSystem) _cachedRenderSystems.Remove(id);
        }
    }
}
