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
    public partial class World
    {
        public Action<World>? OnDestroyed;
        public Action? OnPaused;
        public Action? OnResumed;

        public ComponentLookupTable Lookup { get; set; }

        public Node Root { get; private set; }
        public Dictionary<ulong, Node> Nodes { get; set; }

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

        private bool _destroying = false;

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
                    Order = i
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

            // O(n) loop, try optimize later
            foreach ((int _, Context c) in _contexts)
            {
                c.TryRegisterNode(node);
            }
        }

        internal void UnregisterNode(Node node)
        {
            Nodes.Remove(node.Id);
        }

        public void Pause()
        {
            foreach (var id in _pauseSystems)
            {
                SystemInfo info = _systems[id];
                info.IsActive = false;
                _systemsToResume.Add(id);
            }

            OnPaused?.Invoke();
        }

        public void Resume()
        {
            foreach (var id in _systemsToResume)
            {
                SystemInfo info = _systems[id];
                info.IsActive = true;
            }
            _systemsToResume.Clear();

            OnResumed?.Invoke();
        }

        public void Destroy()
        {
            if (_destroying)
                return;
            _destroying = true;

            Root.Destroy();
            OnDestroyed?.Invoke(this);
        }
    }
}
