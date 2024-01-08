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


        private readonly HashSet<ulong> _entities;
        private readonly ImmutableDictionary<AccessFilter, ImmutableArray<ulong>> _targetComponents;
        private readonly ImmutableDictionary<AccessKind, ImmutableHashSet<ulong>> _operations;

        public bool IsNoFilter => _targetComponents.ContainsKey(AccessFilter.NoFilter);

        private ComponentLookupTable _lookup;

        public Context(World world, ISystem system)
        {
            _lookup = world.Lookup;

            var filters = CreateFilters(system);
            _targetComponents = CreateTargetComponents(filters);
            _operations = CreateOperationsKind(filters);
        }

        private ImmutableArray<(FilterAttribute, ImmutableArray<ulong>)> CreateFilters(ISystem system)
        {
            var builder = ImmutableArray.CreateBuilder<(FilterAttribute, ImmutableArray<ulong>)>();

            FilterAttribute[] filters = (FilterAttribute[])system.GetType()
                .GetCustomAttributes(typeof(FilterAttribute), true);

            foreach (var filter in filters)
            {
                builder.Add((filter, filter.Types.Select(t => _lookup[t]).ToImmutableArray()));
            }

            return builder.ToImmutableArray();
        }

        private ImmutableDictionary<AccessFilter, ImmutableArray<ulong>> CreateTargetComponents(
            ImmutableArray<(FilterAttribute, ImmutableArray<ulong>)> filters)
        {
            var builder = ImmutableDictionary.CreateBuilder<AccessFilter, ImmutableArray<ulong>>();

            foreach (var (filter, targets) in filters)
            {
                if (filter.Filter is AccessFilter.NoFilter)
                {
                    // No-op just set no filter 
                    builder[AccessFilter.NoFilter] = ImmutableArray<ulong>.Empty;
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

        private ImmutableDictionary<AccessKind, ImmutableHashSet<ulong>> CreateOperationsKind(
            ImmutableArray<(FilterAttribute, ImmutableArray<ulong>)> filters)
        {
            var builder = ImmutableDictionary.CreateBuilder<AccessKind, ImmutableHashSet<ulong>>();

            // set default empty hashset 
            builder[AccessKind.Read] = ImmutableHashSet<ulong>.Empty;
            builder[AccessKind.Write] = ImmutableHashSet<ulong>.Empty;

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

        public void TryRegisterNode(Node node)
        {
            if (IsValid(node))
            {
                _entities.Add(node.Id);
            }
        }
    }
}
