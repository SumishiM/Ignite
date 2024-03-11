using Ignite.Attributes;
using Ignite.Components;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Xml.Linq;

namespace Ignite.Systems
{
    public class Context : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        public enum AccessFilter
        {
            NoFilter,
            /// <summary>
            /// Filter systems with any of the components listed
            /// </summary>
            AnyOf,
            /// <summary>
            /// Filter systems with all of the components listed
            /// </summary>
            AllOf,
            /// <summary>
            /// Filter systems with none of the components listed
            /// </summary>
            NoneOf,
        }

        /// <summary>
        /// 
        /// </summary>
        [Flags]
        public enum AccessKind
        {
            Read = 1,
            Write = 2,
        }

        internal event Action<Node, int>? OnNodeComponentAddedInContext;
        internal event Action<Node, int, bool>? OnNodeComponentRemovedInContext;
        internal event Action<Node, int>? OnNodeComponentModifiedInContext;
        internal event Action<Node>? OnNodeEnabledInContext;
        internal event Action<Node>? OnNodeDisabledInContext;

        private readonly int _id;
        internal int Id => _id;

        /// <summary>
        /// Nodes tracked by the context
        /// </summary>
        private readonly Dictionary<ulong, Node> _nodes = [];
        private ImmutableArray<Node>? _cachedNodes = null;

        /// <summary>
        /// Tracked components per types for each node
        /// </summary>
        private readonly Dictionary<int, Dictionary<ulong, IComponent>> _components = [];
        private ImmutableDictionary<Type, ImmutableArray<IComponent>>? _cachedComponents = null;

        /// <summary>
        /// Components filtered in context. Note that this operation can take a bit of time if it's the first time since context modification.
        /// </summary>
        public ImmutableDictionary<Type, ImmutableArray<IComponent>> Components
        {
            get
            {
                _cachedComponents ??= _components.ToImmutableDictionary(
                    kvp => _lookup.GetTypeFromIndex(kvp.Key),
                    kvp => kvp.Value.Where(kvp => !_disabledNodes.Contains(kvp.Key))
                        .ToDictionary().Values
                        .ToImmutableArray());

                return _cachedComponents;
            }
        }

        /// <summary>
        /// Every nodes filtered in context
        /// </summary>
        public ImmutableArray<Node> Nodes
        {
            get
            {
                _cachedNodes ??= [.. _nodes.Values];
                return _cachedNodes.Value;
            }
        }

        /// <summary>
        /// Track every deactivated nodes index from <see cref="_nodes"/>
        /// </summary>
        private readonly HashSet<ulong> _disabledNodes = [];


        /// <summary>
        /// Components targeted by this context filter
        /// </summary>
        private readonly ImmutableDictionary<AccessFilter, ImmutableHashSet<int>> _targetComponents;

        /// <summary>
        /// Components sorted by <see cref="AccessKind"/>
        /// </summary>
        private readonly ImmutableDictionary<AccessKind, ImmutableHashSet<int>> _componentsAccess;

        internal ImmutableHashSet<int> ReadComponents => _componentsAccess[AccessKind.Read];
        internal ImmutableHashSet<int> WriteComponents => _componentsAccess[AccessKind.Write];
        internal ImmutableHashSet<int> FilteredComponents { get; private set; }

        /// <summary>
        /// Whether the world have a filter or not
        /// </summary>
        public bool IsNoFilter => _targetComponents.ContainsKey(AccessFilter.NoFilter);

        private readonly ComponentLookupTable _lookup;

        public Context(World world, ISystem system)
        {
            _lookup = world.Lookup;

            var filters = CreateFilters(system);
            _targetComponents = CreateTargetComponents(filters);
            _componentsAccess = CreateOperationsKind(filters);

            FilteredComponents = [.. _componentsAccess[AccessKind.Read], .. _componentsAccess[AccessKind.Write]];

            _id = GetOrCreateId();
        }

        public Context(World world, AccessFilter filter, params int[] components)
        {
            _lookup = world.Lookup;

            _targetComponents = new Dictionary<AccessFilter, ImmutableHashSet<int>>()
            {
                { filter, components.ToImmutableHashSet() }
            }.ToImmutableDictionary();

            _componentsAccess = ImmutableDictionary<AccessKind, ImmutableHashSet<int>>.Empty;
            FilteredComponents = [.. components];

            _id = GetOrCreateId();
        }

        private int GetOrCreateId()
        {
            List<int> components = new();
            var orderedComponentsFilter = _targetComponents.OrderBy(kvp => kvp.Key);

            foreach (var (filter, collection) in orderedComponentsFilter)
            {
                components.Add(-(int)filter);
                components.AddRange(collection.Order());
            }

            // hash [-1000, 1000]

            int result = 0;
            int shift = 0;

            foreach (var v in components)
            {
                shift = (shift + 11) % 21;
                result ^= (v + 1024) << shift;
            }

            return result;
        }

        /// <summary>
        /// Create context filters for a given <see cref="Ignite.Systems.ISystem"/>
        /// </summary>
        /// <returns>A list of <see cref="Ignite.Attributes.FilterComponentAttribute"/> associated 
        /// with a list of <see cref="Ignite.Components.IComponent"/> indices set by the filter.</returns>
        private ImmutableArray<(FilterComponentAttribute, ImmutableHashSet<int>)> CreateFilters(ISystem system)
        {
            var builder = ImmutableArray.CreateBuilder<(FilterComponentAttribute, ImmutableHashSet<int>)>();

            RequireComponentAttribute[] requirements = [];
            FilterComponentAttribute[] filters = (FilterComponentAttribute[])system.GetType()
                .GetCustomAttributes(typeof(FilterComponentAttribute), true);


            foreach (var filter in filters)
            {
                // add all types filtered by attribute
                builder.Add((filter, filter.Types.Select(t => _lookup[t]).ToImmutableHashSet()));

                foreach (var type in filter.Types)
                {
                    // add required components types for filtered components
                    requirements = (RequireComponentAttribute[])type.GetType()
                        .GetCustomAttributes(typeof(RequireComponentAttribute), true);

                    foreach (var required in requirements)
                    {
                        builder.Add((filter, required.Types.Select(t => _lookup[t]).ToImmutableHashSet()));
                    }
                }
            }


            return builder.ToImmutableArray();
        }

        /// <summary>
        /// Sort the filtered <see cref="Ignite.Components.IComponent"/> indices by the 
        /// <see cref="Ignite.Systems.ISystem"/> <see cref="AccessFilter"/>
        /// </summary>
        /// <param name="filters">
        /// A list of <see cref="Ignite.Attributes.FilterComponentAttribute"/> associated 
        /// with a list of <see cref="Ignite.Components.IComponent"/> indices set by the filter.
        /// </param>
        /// <returns>
        /// A dictionary of list of <see cref="Ignite.Components.IComponent"/> indices 
        /// sorted by <see cref="AccessFilter"/>
        /// </returns>
        private ImmutableDictionary<AccessFilter, ImmutableHashSet<int>> CreateTargetComponents(
            ImmutableArray<(FilterComponentAttribute, ImmutableHashSet<int>)> filters)
        {
            var builder = ImmutableDictionary.CreateBuilder<AccessFilter, ImmutableHashSet<int>>();

            foreach (var (filter, targets) in filters)
            {
                if (filter.Filter is AccessFilter.NoFilter)
                {
                    // No-op just set no filter 
                    builder[AccessFilter.NoFilter] = ImmutableHashSet<int>.Empty;
                    continue;
                }

                if (targets.IsEmpty)
                    // No-op there is no targets to add
                    continue;

                if (builder.TryGetValue(filter.Filter, out var value))
                {
                    // Add targets to the other targets
                    builder[filter.Filter] = value.Union(targets).ToImmutableHashSet();
                }
                else
                {
                    builder[filter.Filter] = targets;
                }
            }

            return builder.ToImmutableDictionary();
        }

        /// <summary>
        /// Sort the filtered <see cref="Ignite.Components.IComponent"/> index by the 
        /// <see cref="Ignite.Systems.ISystem"/> <see cref="AccessKind"/>
        /// </summary>
        /// <param name="filters">
        /// A list of <see cref="Ignite.Attributes.FilterComponentAttribute"/> associated 
        /// with a list of <see cref="Ignite.Components.IComponent"/> index set by the filter.
        /// </param>
        /// <returns>
        /// A dictionary of <see cref="Ignite.Components.IComponent"/> indices 
        /// sorted by <see cref="AccessKind"/>
        /// </returns>
        private ImmutableDictionary<AccessKind, ImmutableHashSet<int>> CreateOperationsKind(
            ImmutableArray<(FilterComponentAttribute, ImmutableHashSet<int>)> filters)
        {
            var builder = ImmutableDictionary.CreateBuilder<AccessKind, ImmutableHashSet<int>>();

            // set default empty hashset 
            builder[AccessKind.Read] = ImmutableHashSet<int>.Empty;
            builder[AccessKind.Write] = ImmutableHashSet<int>.Empty;

            foreach (var (filter, targets) in filters)
            {
                AccessKind kind = filter.Kind;
                if (kind.HasFlag(AccessKind.Write))
                {
                    // Not much we can do by knowing it can be read if we can write
                    kind = AccessKind.Write;
                }

                builder[kind] = builder[kind].Union(targets);
            }

            return builder.ToImmutableDictionary();
        }

        public IEnumerable<T> Get<T>() where T : IComponent
        {
            Debug.Assert(FilteredComponents.Contains(_lookup[typeof(T)]), $"{typeof(T).Name} isn't filtered by this context !");

            return Components[typeof(T)].Cast<T>();
        }

        public IEnumerable<(T1, T2)> Get<T1, T2>()
            where T1 : IComponent
            where T2 : IComponent
        {
            Debug.Assert(FilteredComponents.Contains(_lookup[typeof(T1)]), $"{typeof(T1).Name} isn't filtered by this context !");
            Debug.Assert(FilteredComponents.Contains(_lookup[typeof(T2)]), $"{typeof(T2).Name} isn't filtered by this context !");

            return (IEnumerable<(T1, T2)>)Components[typeof(T1)].Zip(Components[typeof(T2)]);
        }


        /// <summary>
        /// Check whether a <see cref="Ignite.Node"/> is accepted by this <see cref="Context"/> criteria or not
        /// </summary>
        private bool IsValid(Node node)
        {
            if (_targetComponents.TryGetValue(AccessFilter.NoneOf, out var components))
            {
                foreach (var index in components)
                {
                    if (node.HasComponent(index))
                        return false;
                }
            }

            if (_targetComponents.TryGetValue(AccessFilter.AnyOf, out components))
            {
                foreach (var index in components)
                {
                    if (node.HasComponent(index))
                        return true;
                }
            }

            if (_targetComponents.TryGetValue(AccessFilter.AllOf, out components))
            {
                foreach (var index in components)
                {
                    if (!node.HasComponent(index))
                        return false;
                }
                return true;
            }

            // ?
            return true;
        }

        /// <summary>
        /// Try to register a <see cref="Ignite.Node"/> in this <see cref="Context"/>.
        /// Subscribe context to the node events.
        /// </summary>
        internal void TryRegisterNode(Node node)
        {
            if (IsNoFilter) return;

            node.OnComponentAdded += OnNodeComponentAdded;
            node.OnComponentRemoved += OnNodeComponentRemoved;

            if (IsValid(node))
            {
                node.OnComponentModified += OnNodeComponentModifiedInContext;
                node.OnComponentRemoved += OnNodeComponentRemovedInContext;

                node.OnEnabled += OnNodeEnabledInContext;
                node.OnDisabled += OnNodeDisabledInContext;

                if (OnNodeComponentAddedInContext is not null)
                {
                    if (node.IsEnabled)
                    {
                        foreach (var index in node.ComponentsIndices)
                        {
                            OnNodeComponentAddedInContext?.Invoke(node, index);
                        }
                    }

                    node.OnComponentAdded += OnNodeComponentAddedInContext;
                }

                if (node.IsEnabled)
                {
                    _nodes[node.Id] = node;
                    // may not be optimize but do the work
                    foreach (var index in FilteredComponents)
                    {
                        _components[index].TryAdd(node.Id, node.Components[index]);
                    }

                    _cachedComponents = null;
                    _cachedNodes = null;
                }
            }
        }

        internal bool IsWatchingNode(Node node)
            => IsWatchingNode(node.Id);

        internal bool IsWatchingNode(ulong id)
            => _nodes.ContainsKey(id);

        internal void OnNodeComponentAdded(Node node, int component)
        {
            if (node.IsDestroyed) return;
            OnNodeModified(node, component);
        }

        internal void OnNodeComponentRemoved(Node node, int component, bool fromDestroy)
        {
            if (node.IsDestroyed)
            {
                if (_nodes.ContainsKey(node.Id))
                {
                    StopWatchingNode(node, component, true);
                }
                return;
            }

            OnNodeModified(node, component);
        }

        internal void OnNodeEnabled(Node node)
        {
            if (_nodes.ContainsKey(node.Id))
                return;

            _nodes.Add(node.Id, node);
            _cachedNodes = null;
            _cachedComponents = null;

            OnNodeEnabledInContext?.Invoke(node);
            _disabledNodes.Remove(node.Id);
        }

        internal void OnNodeDisabled(Node node)
        {
            if (!_nodes.ContainsKey(node.Id))
                return;

            _nodes.Remove(node.Id);
            _cachedNodes = null;
            _cachedComponents = null;

            OnNodeDisabledInContext?.Invoke(node);

            _disabledNodes.Add(node.Id);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="component"></param>
        internal void OnNodeModified(Node node, int component)
        {
            bool isFiltered = IsValid(node);
            bool isWatching = IsWatchingNode(node);

            if (isFiltered && !isWatching)
                StartWatchingNode(node, component);
            if (!isFiltered && isWatching)
                StopWatchingNode(node, component, false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="component"></param>
        internal void StartWatchingNode(Node node, int component)
        {
            // register components events
            node.OnComponentAdded += OnNodeComponentAddedInContext;
            node.OnComponentRemoved += OnNodeComponentRemovedInContext;
            node.OnComponentModified += OnNodeComponentModifiedInContext;

            // register node events
            node.OnEnabled += OnNodeEnabledInContext;
            node.OnDisabled += OnNodeDisabledInContext;

            if (node.IsEnabled)
            {
                OnNodeComponentAddedInContext?.Invoke(node, component);

                // set node as enable
                _nodes.Add(node.Id, node);

                // reset cache
                _cachedNodes = null;
                _cachedComponents = null;
            }
            else
            {
                // set node as disable
                _disabledNodes.Add(node.Id);
            }
        }

        /// <summary>
        /// End node watching, removing it from context
        /// </summary>
        /// <param name="node">Watched <see cref="Node"/></param>
        /// <param name="component">Component index</param>
        /// <param name="fromDestroy"></param>
        private void StopWatchingNode(Node node, int component, bool fromDestroy)
        {
            // unsubscribe actions
            node.OnComponentAdded -= OnNodeComponentAddedInContext;
            node.OnComponentRemoved -= OnNodeComponentRemovedInContext;
            node.OnComponentModified -= OnNodeComponentModifiedInContext;

            node.OnEnabled -= OnNodeEnabledInContext;
            node.OnDisabled -= OnNodeDisabledInContext;

            if (node.IsEnabled)
            {
                OnNodeComponentRemovedInContext?.Invoke(node, component, fromDestroy);
            }
            else
            {
                //Debug.Assert(!_nodes.ContainsKey(node.Id),
                //    $"Why is a disabled node in the collection?");

                // remove from disabled if disabled
                _disabledNodes.Remove(node.Id);
            }

            // remove node and components
            _nodes.Remove(node.Id);
            foreach (var (_, components) in _components)
            {
                components.Remove(node.Id);
            }

            // reset cache
            _cachedNodes = null;
            _cachedComponents = null;
        }



        public void Dispose()
        {
            OnNodeComponentAddedInContext = null;
            OnNodeComponentRemovedInContext = null;
            OnNodeComponentModifiedInContext = null;

            OnNodeEnabledInContext = null;
            OnNodeDisabledInContext = null;

            _components.Clear();
            _cachedComponents = null;

            _nodes.Clear();
            _cachedNodes = null;
        }
    }
}
