using Ignite.Attributes;
using Ignite.Components;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.InteropServices;

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


        private readonly HashSet<int> _entities;
        private readonly ImmutableDictionary<AccessFilter, ImmutableArray<int>> _targetComponents;
        private readonly ImmutableDictionary<AccessKind, ImmutableHashSet<int>> _operations;

        public bool IsNoFilter => _targetComponents.ContainsKey(AccessFilter.NoFilter);

        private ComponentLookupTable _lookup;

        public Context(World world, ISystem system)
        {
            _lookup = world.Lookup;

            var filters = CreateFilters(system);
            _targetComponents = CreateTargetComponents(filters);
            _operations = CreateOperationsKind(filters);
        }

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
    }
}
