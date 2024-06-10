using Ignite.Systems;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO.Compression;

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
        private readonly SortedList<int, (IExitSystem system, int context)> _cachedExitSystems;

        /// <summary>
        /// Systems executed on world update
        /// </summary>
        private readonly SortedList<int, (IUpdateSystem system, int context)> _cachedUpdateSystems;

        /// <summary>
        /// Systems executed on world update
        /// </summary>
        private readonly SortedList<int, (IFixedUpdateSystem system, int context)> _cachedFixedUpdateSystems;

        /// <summary>
        /// Systems executed on world render
        /// </summary>
        private readonly SortedList<int, (IRenderSystem system, int context)> _cachedRenderSystems;

        /// <summary>
        /// System infos for each registered system.
        /// </summary>
        private readonly Dictionary<int, SystemInfo> _systems;

        /// <summary>
        /// Systems from id
        /// </summary>
        private readonly ImmutableDictionary<int, ISystem> _idToSystems;

        /// <summary>
        /// Types to system id
        /// </summary>
        internal readonly ImmutableDictionary<Type, int> TypeToSystem;

        /// <summary>
        /// Ids of the systems that can pause
        /// </summary>
        private readonly ImmutableHashSet<int> _pauseSystems;

        /// <summary>
        /// Ids of the systems that cannot pause
        /// </summary>
        private readonly ImmutableHashSet<int> _ignorePauseSystems;

        /// <summary>
        /// Ids of <see cref="IStartSystem"/> initialized.
        /// </summary>
        private readonly HashSet<int> _systemsInitialized;

        /// <summary>
        /// Systems ids of systems waiting to be toggled and there current pause state
        /// </summary>
        private readonly Dictionary<int, bool> _pendingToggleSystems;

        /// <summary>
        /// Contexts created for the systems in the world
        /// </summary>
        private readonly Dictionary<int, Context> _contexts;

        /// <summary>
        /// Systems types wanting to be added in world
        /// </summary>
        private readonly List<Type> _pendingAddSystems;

        /// <summary>
        /// Systems ids wanting to be removed
        /// </summary>
        private readonly HashSet<int> _pendingRemoveSystems;

        private int _lastSystemId;

        private void AddSystem<T>(bool immediate = false) where T : ISystem
            => AddSystem(typeof(T), immediate);

        private void AddSystem(Type type, bool immediate = false)
        {
            if (!immediate)
            {
                if (!_pendingAddSystems.Contains(type))
                    _pendingAddSystems.Add(type);
                return;
            }

            if (Activator.CreateInstance(type) is not ISystem system)
                return;

            Context context = new(this, system);
            int id = ++_lastSystemId;

            // check context
            if (_contexts.TryGetValue(context.Id, out Context? value))
            {
                context = value;
            }
            else
            {
                _contexts.Add(context.Id, context);
            }


            // pause
            if (DoSystemIgnorePause(system))
            {
                _ignorePauseSystems.Add(id);
            }
            else if (CanSystemPause(system))
            {
                _pauseSystems.Add(id);
            }

            _idToSystems.Add(id, system);
            _systems.Add(id, new SystemInfo
            {
                ContextId = context.Id,
                Index = id,
                Order = id // maybe make an algo to check systems dependency on other systems
            });

            TypeToSystem.Add(system.GetType(), id);

            if (system is IStartSystem startSystem) _cachedStartSystems.Add(id, (startSystem, context.Id));
            if (system is IUpdateSystem updateSystem) _cachedUpdateSystems.Add(id,(updateSystem, context.Id));
            if (system is IFixedUpdateSystem fixedUpdateSystem) _cachedFixedUpdateSystems.Add(id, (fixedUpdateSystem, context.Id));
            if (system is IRenderSystem renderSystem) _cachedRenderSystems.Add(id, (renderSystem, context.Id));
            if (system is IExitSystem exitSystem) _cachedExitSystems.Add(id, (exitSystem, context.Id));
        }

        public void RemoveSystem<T>(bool immediate = false) where T : ISystem
            => RemoveSystem(typeof(T), immediate);

        public void RemoveSystem(Type type, bool immediate = false)
        {
            if (!TypeToSystem.TryGetValue(type, out int id))
                return; // ? system not registered in world

            // if not immediate, we register the system id to wait for remove
            if (!immediate)
            {
                _pendingRemoveSystems.Add(id);
                return;
            }

            // check if any other system uses the same context
            if (_systems.Any(s => s.Key != id && s.Value.ContextId == _systems[id].ContextId))
            {
                // remove context related to the system
                _contexts.Remove(_systems[id].ContextId);
            }

            //remove the system from world cache 
            _systems.Remove(id);
            _idToSystems.Remove(id);
            TypeToSystem.Remove(type);
            _systemsInitialized.Remove(id);

            if (_cachedStartSystems.ContainsKey(id)) _cachedStartSystems.Remove(id);
            if (_cachedUpdateSystems.ContainsKey(id)) _cachedUpdateSystems.Remove(id);
            if (_cachedFixedUpdateSystems.ContainsKey(id)) _cachedFixedUpdateSystems.Remove(id);
            if (_cachedRenderSystems.ContainsKey(id)) _cachedRenderSystems.Remove(id);
            if (_cachedExitSystems.ContainsKey(id)) _cachedExitSystems.Remove(id);
        }

        private void RemovePendingSystems()
        {
            foreach (var systemId in _pendingRemoveSystems)
            {
                RemoveSystem(_idToSystems[systemId].GetType(), true);
            }
        }



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

            if (!TypeToSystem.TryGetValue(type, out int id))
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

            if (!TypeToSystem.TryGetValue(type, out int id))
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
            if (system is IFixedUpdateSystem fixedUpdatepdateSystem) _cachedFixedUpdateSystems.Add(id, (fixedUpdatepdateSystem, contextId));
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
            if (system is IFixedUpdateSystem) _cachedFixedUpdateSystems.Remove(id);
            if (system is IRenderSystem) _cachedRenderSystems.Remove(id);
        }

        /// <summary>
        /// Get the id of a context already in the world or create a new context
        /// </summary>
        private int GetOrCreateContext(Context.AccessFilter filter, params int[] components)
        {
            Context context = new(this, filter, components);
            if (_contexts.ContainsKey(context.Id))
            {
                return context.Id;
            }

            foreach (var (_, node) in Nodes)
            {
                context.TryRegisterNode(node);
            }

            _contexts.Add(context.Id, context);
            return context.Id;
        }

        /// <summary>
        /// Get the id of a context already in the world or create a new context
        /// </summary>
        private int GetOrCreateContext(Context.AccessFilter filter, params Type[] components)
            => GetOrCreateContext(filter, components.Select(t => Lookup[t]).ToArray());
    }
}
