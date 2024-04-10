using Ignite.Attributes;
using Ignite.Components;
using Ignite.Systems;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Security.Principal;
using static Ignite.Node;

namespace Ignite
{
    public partial class World : IDisposable
    {
        // events
        public Action<World>? OnDestroyed;
        public Action? OnPaused;
        public Action? OnResumed;

        public ComponentLookupTable Lookup { get; set; }

        // states
        private bool _isExiting = false;
        public bool IsExiting => _isExiting;

        private bool _isPaused = false;
        public bool IsPaused => _isPaused;

        internal UIDGenerator _UIDGenerator;

        internal class UIDGenerator
        {
            public ulong LastGeneratedId { get; private set; } = 0;

            private ulong CurrentId = 0;
            private ushort CurrentGenerationId = 0;

            public ulong Next(Node.Flags flags = Flags.Empty)
            {
                ulong id = (ulong)flags;

                if (++CurrentId < UInt32.MaxValue)
                    id += CurrentId;
                else
                {
                    CurrentId = 0;
                    if (++CurrentGenerationId < UInt16.MaxValue)
                        id += CurrentGenerationId;
                    else
                        CurrentGenerationId = 0;
                }

                LastGeneratedId = id;
                return id;
            }
        }


        public World(IList<ISystem> systems)
        {
            Nodes = [];

            _UIDGenerator = new UIDGenerator();

            Lookup = FindLookupTableImplementation();
            Lookup.CheckRequirements();

            _systemsInitialized = [];
            _pendingToggleSystems = [];


            var pauseSystems = ImmutableHashSet.CreateBuilder<int>();
            var ignorePauseSystems = ImmutableHashSet.CreateBuilder<int>();

            var idToSystems = ImmutableDictionary.CreateBuilder<int, ISystem>();

            _contexts = [];
            _systems = [];

            for (int i = 0; i < systems.Count; i++)
            {
                ISystem system = systems[i];
                Context context = new(this, system);

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
                    Order = i // maybe make an algo to check systems dependency on other systems
                });
            }

            _pauseSystems = pauseSystems.ToImmutable();
            _ignorePauseSystems = ignorePauseSystems.ToImmutable();

            _idToSystems = idToSystems.ToImmutable();
            TypeToSystem = idToSystems.ToImmutableDictionary(system => system.Value.GetType(), system => system.Key);

            // cache systems from type
            _cachedStartSystems = new(_systems.Where(kvp => _idToSystems[kvp.Key] is IStartSystem)
                .ToDictionary(kvp => kvp.Value.Order, kvp => ((IStartSystem)_idToSystems[kvp.Key], kvp.Value.ContextId)));

            _cachedUpdateSystems = new(_systems.Where(kvp => _idToSystems[kvp.Key] is IUpdateSystem)
                .ToDictionary(kvp => kvp.Value.Order, kvp => ((IUpdateSystem)_idToSystems[kvp.Key], kvp.Value.ContextId)));

            _cachedFixedUpdateSystems = new(_systems.Where(kvp => _idToSystems[kvp.Key] is IFixedUpdateSystem)
                .ToDictionary(kvp => kvp.Value.Order, kvp => ((IFixedUpdateSystem)_idToSystems[kvp.Key], kvp.Value.ContextId)));

            _cachedRenderSystems = new(_systems.Where(kvp => _idToSystems[kvp.Key] is IRenderSystem)
                .ToDictionary(kvp => kvp.Value.Order, kvp => ((IRenderSystem)_idToSystems[kvp.Key], kvp.Value.ContextId)));

            _cachedExitSystems = new(_systems.Where(kvp => _idToSystems[kvp.Key] is IExitSystem)
                .ToDictionary(kvp => kvp.Value.Order, kvp => ((IExitSystem)_idToSystems[kvp.Key], kvp.Value.ContextId)));


            Root = Node.CreateBuilder(this, "Root").ToNode();
        }

        /// <summary>
        /// Pause systems that can be paused. Those systems will be disabled at the end of the frame.
        /// </summary>
        public void Pause()
        {
            _isPaused = true;

            foreach (var id in _pauseSystems)
            {
                DisableSystem(id);
            }

            OnPaused?.Invoke();
        }

        /// <summary>
        /// Resume systems waiting to be resumed. Those systems will be enabled at the begining of the frame
        /// World is set as resumed once every systems that can be are.
        /// </summary>
        public void Resume()
        {
            foreach (var id in _pauseSystems)
            {
                EnableSystem(id);
            }

            foreach (var id in _ignorePauseSystems)
            {
                if(!_systems[id].IsActive)
                    EnableSystem(id);
            }

            _isPaused = false;
            OnResumed?.Invoke();
        }

        /// <summary>
        /// Execute every registered <see cref="IStartSystem"/>
        /// </summary>
        public void Start()
        {
            foreach (var (id, (system, contextId)) in _cachedStartSystems)
            {
                system.Start(_contexts[contextId]);
                _systemsInitialized.Add(id);
            }
        }

        /// <summary>
        /// Execute every registered not paused <see cref="IUpdateSystem"/>
        /// </summary>
        public void Update()
        {
            foreach (var (id, (system, contextId)) in _cachedUpdateSystems)
            {
                if (_isPaused && _pauseSystems.Contains(id))
                    continue;

                Context context = _contexts[contextId];
                system.Update(context);
            }

            DestroyPendingNodes();
            TogglePendingSystems();
        }

        public void FixedUpdate()
        {
            foreach(var (id, (system, contextId)) in _cachedFixedUpdateSystems)
            {
                if (_isPaused && _pauseSystems.Contains(id))
                    continue;

                Context context = _contexts[contextId];
                system.FixedUpdate(context);
            }
        }

        /// <summary>
        /// Execute every registered <see cref="IRenderSystem"/>
        /// </summary>
        public void Render()
        {
            foreach (var (_, (system, contextId)) in _cachedRenderSystems)
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

            foreach (var (system, contextId) in _cachedExitSystems.Values)
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

            _idToSystems.Clear();

            _cachedStartSystems.Clear();
            _cachedUpdateSystems.Clear();
            _cachedFixedUpdateSystems.Clear();
            _cachedRenderSystems.Clear();
            _cachedExitSystems.Clear();

            foreach (var node in Nodes.Values)
            {
                node.Dispose();
            }

            foreach (var context in _contexts.Values)
            {
                context.Dispose();
            }

            _cachedLookupTableImplementation = null;

            GC.SuppressFinalize(this);
        }

    }
}
