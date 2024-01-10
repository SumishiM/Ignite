using Ignite.Components;
using Ignite.Systems;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Security.Principal;

namespace Ignite
{
    /// <summary>
    /// <para>
    /// Whats left ?
    /// Register nodes automatically
    /// Get lookup table
    /// </para>
    /// </summary>
    public partial class World : IDisposable
    {
        // events

        public Action<World>? OnDestroyed;
        public Action? OnPaused;
        public Action? OnResumed;

        // Node / hierarchy
        public Node Root { get; private set; }
        public readonly Dictionary<ulong, Node> Nodes;
        private readonly HashSet<Node> _pendingDestroyNodes = [];


        // systems
        private readonly SortedList<int, (IStartSystem system, int context)> _cachedStartSystem;
        private readonly SortedList<int, (IExitSystem system, int context)> _cachedExitSystem;
        private readonly SortedList<int, (IUpdateSystem system, int context)> _cachedUpdateSystem;
        private readonly SortedList<int, (IRenderSystem system, int context)> _cachedRenderSystem;

        private readonly Dictionary<int, SystemInfo> _systems;
        private readonly ImmutableDictionary<int, ISystem> _idToSystems;
        private readonly ImmutableDictionary<Type, int> _typeToSystem;

        private readonly ImmutableHashSet<int> _pauseSystems;
        private readonly ImmutableHashSet<int> _ignorePauseSystems;
        private readonly HashSet<int> _systemsToResume;
        private readonly HashSet<int> _systemsInitialized;

        private readonly Dictionary<int, Context> _contexts;
        public ComponentLookupTable Lookup { get; set; }

        // states
        private bool _isExiting = false;
        public bool IsExiting => _isExiting;

        private bool _isPaused = false;
        public bool IsPaused => _isPaused;


        public World(IList<ISystem> systems)
        {
            Root = Node.CreateBuilder(this).ToNode();


            var pauseSystems = ImmutableHashSet.CreateBuilder<int>();
            var ignorePauseSystems = ImmutableHashSet.CreateBuilder<int>();

            var idToSystems = ImmutableDictionary.CreateBuilder<int, ISystem>();

            _contexts = new Dictionary<int, Context>();
            _systems = new Dictionary<int, SystemInfo>();

            for (int i = 0; i < systems.Count; i++)
            {
                ISystem system = systems[i];
                Context context = new(this, system);

                if (_contexts.TryGetValue(context.Id, out Context? value))
                {
                    context = value;
                }
                else
                {
                    _contexts.Add(context.Id, context);
                }

                if (DoSystemIgnorePause(system))
                {
                    ignorePauseSystems.Add(i);
                }
                else if (CanSystemPause(system))
                {
                    pauseSystems.Add(i);
                }

                idToSystems.Add(i, system);
                _systems.Add(i, new SystemInfo
                {
                    ContextId = context.Id,
                    Index = i,
                    Order = i // maybe we can make an algo to check systems dependency on other systems
                });
            }

            _pauseSystems = pauseSystems.ToImmutable();
            _ignorePauseSystems = ignorePauseSystems.ToImmutable();

            _idToSystems = idToSystems.ToImmutable();
            _typeToSystem = idToSystems.ToImmutableDictionary(system => system.Value.GetType(), system => system.Key);

            _cachedStartSystem = new(_systems.Where(kvp => _idToSystems[kvp.Key] is IStartSystem)
                .ToDictionary(kvp => kvp.Value.Order, kvp => ((IStartSystem)_idToSystems[kvp.Key], kvp.Value.ContextId)));

            _cachedUpdateSystem = new(_systems.Where(kvp => _idToSystems[kvp.Key] is IUpdateSystem)
                .ToDictionary(kvp => kvp.Value.Order, kvp => ((IUpdateSystem)_idToSystems[kvp.Key], kvp.Value.ContextId)));

            _cachedRenderSystem = new(_systems.Where(kvp => _idToSystems[kvp.Key] is IRenderSystem)
                .ToDictionary(kvp => kvp.Value.Order, kvp => ((IRenderSystem)_idToSystems[kvp.Key], kvp.Value.ContextId)));

            _cachedExitSystem = new(_systems.Where(kvp => _idToSystems[kvp.Key] is IExitSystem)
                .ToDictionary(kvp => kvp.Value.Order, kvp => ((IExitSystem)_idToSystems[kvp.Key], kvp.Value.ContextId)));
        }

        internal void RegisterNode(Node node)
        {
            Debug.Assert(!Nodes.TryAdd(node.Id, node),
                $"A node with this Id ({node.Id}) is already registered in the world !");

            if (node.Parent == null)
                node.SetParent(Root);

            // O(n) loop, try optimize later maybe probably
            foreach ((int _, Context c) in _contexts)
            {
                c.TryRegisterNode(node);
            }
        }

        internal void UnregisterNode(Node node)
        {
            Nodes.Remove(node.Id);
        }

        internal void TagForDestroy(Node node)
        {
            Nodes.Remove(node.Id);
            // update tags on node id
            _pendingDestroyNodes.Add(node);
        }

        private void DestroyPendingNodes()
        {
            foreach (var node in _pendingDestroyNodes)
            {
                node.Dispose();
            }
        }

        /// <summary>
        /// Pause systems that can be paused
        /// </summary>
        public void Pause()
        {
            foreach (var id in _pauseSystems)
            {
                SystemInfo info = _systems[id];
                info.IsActive = false;
                _systemsToResume.Add(id);
            }

            _isPaused = true;
            OnPaused?.Invoke();
        }

        /// <summary>
        /// Resume systems waiting to be resumed
        /// </summary>
        public void Resume()
        {
            foreach (var id in _systemsToResume)
            {
                SystemInfo info = _systems[id];
                info.IsActive = true;
            }
            _systemsToResume.Clear();

            _isPaused = false;
            OnResumed?.Invoke();
        }

        /// <summary>
        /// Execute every registered <see cref="IStartSystem"/>
        /// </summary>
        public void Start()
        {
            foreach ((IStartSystem system, int contextId) in _cachedStartSystem.Values)
            {
                system.Start(_contexts[contextId]);
            }
        }

        /// <summary>
        /// Execute every registered not paused <see cref="IUpdateSystem"/>
        /// </summary>
        public void Update()
        {
            foreach ((int systemId, (IUpdateSystem system, int contextId)) in _cachedUpdateSystem)
            {
                if (_isPaused && _pauseSystems.Contains(systemId))
                    continue;

                system.Update(_contexts[contextId]);
            }
        }

        /// <summary>
        /// Execute every registered <see cref="IRenderSystem"/>
        /// </summary>
        public void Render()
        {
            foreach ((IRenderSystem system, int contextId) in _cachedRenderSystem.Values)
            {
                system.Render(_contexts[contextId]);
            }
        }

        /// <summary>
        /// Execute every registered <see cref="IExitSystem"/>
        /// </summary>
        public void Exit()
        {
            if (_isExiting)
                return;
            _isExiting = true;

            foreach ((IExitSystem system, int contextId) in _cachedExitSystem.Values)
            {
                system.Exit(_contexts[contextId]);
            }

            Root.Destroy();
            OnDestroyed?.Invoke(this);
        }

        public void Dispose()
        {
            Exit();

            OnPaused = null;
            OnResumed = null;
            OnDestroyed = null;
        }

    }
}
