using Ignite.Attributes;
using Ignite.Components;
using System.Collections.Immutable;

namespace Ignite.Systems
{
    public class Context
    {
        public enum AccessFilter
        {
            NoFilter,
            AnyOf,
            AllOf,
            NoneOf,
        }

        [Flags]
        public enum AccessKind
        {
            Read = 1,
            Write = 2,
        }

        /// <summary>
        /// Nodes tracked by the context
        /// </summary>
        private readonly Dictionary<ulong, Node> _nodes;

        public ImmutableArray<Node> Nodes
        {
            get
            {
                _cachedNodes ??= [.. _nodes.Values];
                return _cachedNodes.Value;
            }
        }
        
        private readonly HashSet<ulong> _deactivatedNodes;
        private ImmutableArray<Node>? _cachedNodes = null;


        private readonly ImmutableDictionary<AccessFilter, ImmutableArray<int>> _targetComponents;
        private readonly ImmutableDictionary<AccessKind, ImmutableHashSet<int>> _operations;

        internal ImmutableHashSet<int> ReadComponents => _operations[AccessKind.Read];
        internal ImmutableHashSet<int> WriteComponents => _operations[AccessKind.Write];

        public bool IsNoFilter => _targetComponents.ContainsKey(AccessFilter.NoFilter);

        private ComponentLookupTable _lookup;

        public Context(World world, ISystem system)
        {
            _lookup = world.Lookup;

            _nodes = [];
            var filters = CreateFilters(system);
            _targetComponents = CreateTargetComponents(filters);
            _operations = CreateOperationsKind(filters);
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
        /// Try to register a <see cref="Ignite.Node"/> in this <see cref="Context"/>
        /// </summary>
        /// <param name="node"></param>
        public void TryRegisterNode(Node node)
        {
            if (IsValid(node))
            {
                _nodes.Add(node.Id, node);
            }
        }
    }
}
