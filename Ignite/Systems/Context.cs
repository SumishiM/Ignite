using Ignite.Attributes;
using Ignite.Components;
using System.Collections.Immutable;
using System.Diagnostics;

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
            AnyOf,
            AllOf,
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
        private ImmutableArray<Node>? _cachedNodes = null;

        /// <summary>
        /// Components targeted by this context filter
        /// </summary>
        private readonly ImmutableDictionary<AccessFilter, ImmutableArray<int>> _targetComponents;

        /// <summary>
        /// Components sorted by <see cref="AccessKind"/>
        /// </summary>
        private readonly ImmutableDictionary<AccessKind, ImmutableHashSet<int>> _componentsAccess;

        internal ImmutableHashSet<int> ReadComponents => _componentsAccess[AccessKind.Read];
        internal ImmutableHashSet<int> WriteComponents => _componentsAccess[AccessKind.Write];

        public bool IsNoFilter => _targetComponents.ContainsKey(AccessFilter.NoFilter);

        private ComponentLookupTable _lookup;

        public Context(World world, ISystem system)
        {
            _lookup = world.Lookup;

            var filters = CreateFilters(system);
            _targetComponents = CreateTargetComponents(filters);
            _componentsAccess = CreateOperationsKind(filters);

            _id = GetOrCreateId();
        }

        public Context(World world, AccessFilter filter, params int[] components)
        {
            _lookup = world.Lookup;

            _targetComponents = new Dictionary<AccessFilter, ImmutableArray<int>>()
            {
                { filter, components.ToImmutableArray() }
            }.ToImmutableDictionary();

            _componentsAccess = ImmutableDictionary<AccessKind, ImmutableHashSet<int>>.Empty;

            _id = GetOrCreateId();
        }

        private int GetOrCreateId()
        {
            List<int> components = new();
            var orderedComponentsFilter = _targetComponents.OrderBy(kvp => kvp.Key);

            foreach (var (filter, collection) in orderedComponentsFilter)
            {
                components.Add(-(int)filter);
                components.AddRange(collection.Sort());
            }

            // hash

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
        /// <returns>A list of <see cref="Ignite.Attributes.FilterAttribute"/> associated 
        /// with a list of <see cref="Ignite.Components.IComponent"/> indices set by the filter.</returns>
        private ImmutableArray<(FilterAttribute, ImmutableArray<int>)> CreateFilters(ISystem system)
        {
            var builder = ImmutableArray.CreateBuilder<(FilterAttribute, ImmutableArray<int>)>();

            FilterAttribute[] filters = (FilterAttribute[])system.GetType()
                .GetCustomAttributes(typeof(FilterAttribute), true);

            foreach (var filter in filters)
            {
                builder.Add((filter, filter.Types.Select(t => _lookup[t]).ToImmutableArray()));
            }

            return builder.ToImmutableArray();
        }

        /// <summary>
        /// Sort the filtered <see cref="Ignite.Components.IComponent"/> indices by the 
        /// <see cref="Ignite.Systems.ISystem"/> <see cref="AccessFilter"/>
        /// </summary>
        /// <param name="filters">
        /// A list of <see cref="Ignite.Attributes.FilterAttribute"/> associated 
        /// with a list of <see cref="Ignite.Components.IComponent"/> indices set by the filter.
        /// </param>
        /// <returns>
        /// A dictionary of list of <see cref="Ignite.Components.IComponent"/> indices 
        /// sorted by <see cref="AccessFilter"/>
        /// </returns>
        private ImmutableDictionary<AccessFilter, ImmutableArray<int>> CreateTargetComponents(
            ImmutableArray<(FilterAttribute, ImmutableArray<int>)> filters)
        {
            var builder = ImmutableDictionary.CreateBuilder<AccessFilter, ImmutableArray<int>>();

            foreach (var (filter, targets) in filters)
            {
                if (filter.Filter is AccessFilter.NoFilter)
                {
                    // No-op just set no filter 
                    builder[AccessFilter.NoFilter] = ImmutableArray<int>.Empty;
                    continue;
                }

                if (targets.IsDefaultOrEmpty)
                    // No-op there is no targets to add
                    continue;

                if (builder.TryGetValue(filter.Filter, out var value))
                {
                    // Add targets to the other targets
                    builder[filter.Filter] = value.Union(targets).ToImmutableArray();
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
        /// A list of <see cref="Ignite.Attributes.FilterAttribute"/> associated 
        /// with a list of <see cref="Ignite.Components.IComponent"/> index set by the filter.
        /// </param>
        /// <returns>
        /// A dictionary of <see cref="Ignite.Components.IComponent"/> indices 
        /// sorted by <see cref="AccessKind"/>
        /// </returns>
        private ImmutableDictionary<AccessKind, ImmutableHashSet<int>> CreateOperationsKind(
            ImmutableArray<(FilterAttribute, ImmutableArray<int>)> filters)
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
                    if (node.IsActive)
                    {
                        foreach (var index in node.ComponentsIndices)
                        {
                            OnNodeComponentAddedInContext?.Invoke(node, index);
                        }
                    }

                    node.OnComponentAdded += OnNodeComponentAddedInContext;
                }

                if (node.IsActive)
                {
                    _nodes[node.Id] = node;
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
                    StopWatchingNode(node, component, fromDestroy);
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

            OnNodeEnabledInContext?.Invoke(node);
            _disabledNodes.Remove(node.Id);
        }

        internal void OnNodeDisabled(Node node)
        {
            if (!_nodes.ContainsKey(node.Id))
                return;

            _nodes.Remove(node.Id);
            _cachedNodes = null;

            OnNodeDisabledInContext?.Invoke(node);

            _disabledNodes.Add(node.Id);
        }

        internal void OnNodeModified(Node node, int component)
        {
            bool isFiltered = IsValid(node);
            bool isWatching = IsWatchingNode(node);

            if (isFiltered && !isWatching)
                StartWatchingNode(node, component);
            if (!isFiltered && isWatching)
                StopWatchingNode(node, component, false);
        }

        internal void StartWatchingNode(Node node, int component)
        {
            node.OnComponentAdded += OnNodeComponentAddedInContext;
            node.OnComponentRemoved += OnNodeComponentRemovedInContext;
            node.OnComponentModified += OnNodeComponentModifiedInContext;

            node.OnEnabled += OnNodeEnabledInContext;
            node.OnDisabled += OnNodeDisabledInContext;

            if (node.IsActive)
            {
                OnNodeComponentAddedInContext?.Invoke(node, component);

                _nodes.Add(node.Id, node);
                _cachedNodes = null;
            }
            else
            {
                _disabledNodes.Add(node.Id);
            }
        }

        private void StopWatchingNode(Node node, int component, bool fromDestroy)
        {
            node.OnComponentAdded -= OnNodeComponentAddedInContext;
            node.OnComponentRemoved -= OnNodeComponentRemovedInContext;
            node.OnComponentModified -= OnNodeComponentModifiedInContext;

            node.OnEnabled -= OnNodeEnabledInContext;
            node.OnDisabled -= OnNodeDisabledInContext;

            if (node.IsActive)
            {
                OnNodeComponentRemovedInContext?.Invoke(node, component, fromDestroy);
            }
            else
            {
                Debug.Assert(!_nodes.ContainsKey(node.Id),
                    $"Why is a disabled node in the collection?");

                _disabledNodes.Remove(node.Id);
            }

            _nodes.Remove(node.Id);
            _cachedNodes = null;
        }


        public void Dispose()
        {
            OnNodeComponentAddedInContext = null;
            OnNodeComponentRemovedInContext = null;
            OnNodeComponentModifiedInContext = null;

            OnNodeEnabledInContext = null;
            OnNodeDisabledInContext = null;

            _nodes.Clear();
            _cachedNodes = null;
        }
    }
}
