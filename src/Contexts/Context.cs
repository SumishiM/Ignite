using Ignite.Attributes;
using Ignite.Components;
using Ignite.Entities;
using Ignite.Systems;

using System.Collections.Immutable;

namespace Ignite.Contexts
{
    /// <summary>
    /// 
    /// </summary>
    public class Context
    {
        /// <summary>
        /// 
        /// </summary>
        public enum AccessFilter
        {
            /// <summary>
            /// Don't filter entities
            /// </summary>
            NoFilter = 1,

            /// <summary>
            /// Filter entities with every listed components
            /// </summary>
            AllOf = 2,

            /// <summary>
            /// Filter entities with any of the listed components
            /// </summary>
            AnyOf = 3,

            /// <summary>
            /// Filter the components with none of the listed components 
            /// </summary>
            NoneOf = 4
        }

        /// <summary>
        /// 
        /// </summary>
        [Flags]
        public enum AccessKind
        {
            /// <summary>
            /// Allow the system to read data from the listed components
            /// </summary>
            Read = 1,

            /// <summary>
            /// Allow the system to write data to the listed components
            /// </summary>
            Write = 2
        }

        private Dictionary<int, Entity> Entities;
        private HashSet<int> DeactivatedEntities;

        /// <summary>
        /// Ordered components indices by filter types
        /// </summary>
        private readonly ImmutableDictionary<AccessFilter, ImmutableArray<int>> _targetComponentsIndices;

        /// <summary>
        /// Ordered components indices by there accessibility
        /// </summary>
        private readonly ImmutableDictionary<AccessKind, ImmutableHashSet<int>> _componentsOperationKind;

        private readonly ComponentsLookupTable Lookup;

        /// <summary>
        /// Returns whether this context does not have any filter and grab all entities instead.
        /// </summary>
        private bool IsNoFilter => _targetComponentsIndices.ContainsKey(AccessFilter.NoFilter);

        internal Context(/*World world,*/ISystem system)
        {
            var filters = CreateFilters(system);
            _targetComponentsIndices = CreateTargetComponents(filters);
            _componentsOperationKind = CreateAccessKindComponents(filters);
        }

        /// <summary>
        /// Create the filters for the filters attributes of the system
        /// </summary>
        /// <param name="system"></param>
        /// <returns>immutable list of elements containing a 
        /// filter and a list of indicies of components affected by the filter</returns>
        private ImmutableArray<(FilterAttribute, ImmutableArray<int>)> CreateFilters(ISystem system)
        {
            var builder = ImmutableArray.CreateBuilder<(FilterAttribute, ImmutableArray<int>)>();

            // collect filters on the system
            FilterAttribute[] filters = (FilterAttribute[])system
                .GetType().GetCustomAttributes(typeof(FilterAttribute), true);

            foreach (FilterAttribute filter in filters)
            {
                builder.Add((filter, filter.Types.Select(type => Lookup.GetId(type)).ToImmutableArray()));
            }

            // return the builder as immutable array
            // The same as return builder.ToImmutableArray();
            return [.. builder];
        }

        /// <summary>
        /// Order the targeted components by filters
        /// </summary>
        /// <param name="filters"></param>
        /// <returns>Ordered components by filters</returns>
        private static ImmutableDictionary<AccessFilter, ImmutableArray<int>> CreateTargetComponents(
            ImmutableArray<(FilterAttribute, ImmutableArray<int>)> filters)
        {
            var builder = ImmutableDictionary.CreateBuilder<AccessFilter, ImmutableArray<int>>();

            foreach (var (filter, targets) in filters)
            {
                if (filter.Filter is AccessFilter.NoFilter)
                {
                    // Default value for context
                    builder[filter.Filter] = ImmutableArray<int>.Empty;
                    continue;
                }

                if (targets.IsDefaultOrEmpty)
                    // No-op
                    continue;

                // Check if there is already some types stored for the filter
                // If true -> Add to the already stored types
                // If false -> Add the targets
                if (builder.TryGetValue(filter.Filter, out ImmutableArray<int> value))
                {
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
        /// Order every components indices by Read / Write accessibility
        /// </summary>
        /// <param name="filters"></param>
        /// <returns>Ordered components by accessibility</returns>
        private ImmutableDictionary<AccessKind, ImmutableHashSet<int>> CreateAccessKindComponents(
            ImmutableArray<(FilterAttribute, ImmutableArray<int>)> filters)
        {
            var builder = ImmutableDictionary.CreateBuilder<AccessKind, ImmutableHashSet<int>>();

            // set default value for the context to use
            builder[AccessKind.Read] = ImmutableHashSet<int>.Empty;
            builder[AccessKind.Write] = ImmutableHashSet<int>.Empty;

            foreach(var (filter, targets) in filters)
            {
                if (targets.IsDefaultOrEmpty || filter.Filter is AccessFilter.NoneOf)
                    // No-op
                    continue;

                AccessKind access = filter.Kind;

                if (filter.Kind.HasFlag(AccessKind.Write))
                {
                    // We assume that if we can write we can read
                    access = AccessKind.Write;
                }

                // Check whether there is already values stored or not
                // If true -> add to the current list
                // If false -> set targets
                if (builder.TryGetValue(access, out var value))
                {
                    builder[access] = value.Union(targets);
                }
                else
                {
                    // act as a cast from ImmutableArray<int> to ImmutableHashSet<int>
                    // add every elements of targets
                    builder[access] = [.. targets];
                }
            }

            return builder.ToImmutableDictionary();
        }

        public void FilterEntity(Entity entity) 
        {
            if(IsNoFilter)
            {
                //No-op for this context
                return;
            }


        }

        /// <summary>
        /// Check whether the entity is matching the system filters
        /// </summary>
        /// <param name="entity">Entity to check</param>
        /// <returns>Whether the entity is valid or not</returns>
        public bool MatchEntity(Entity entity)
        {
            if (_targetComponentsIndices.TryGetValue(AccessFilter.AnyOf, out var indices))
            {
                foreach (var index in indices)
                {
                    if (entity.HasComponent(index))
                        return true;
                }
            }

            if (_targetComponentsIndices.TryGetValue(AccessFilter.AllOf, out indices))
            {
                foreach (var index in indices)
                {
                    if (!entity.HasComponent(index))
                        return false;
                }

                return true;
            }

            if(_targetComponentsIndices.TryGetValue(AccessFilter.NoneOf, out indices))
            {
                foreach (var index in indices)
                {
                    if (entity.HasComponent(index))
                        return false;
                }
            }

            return true;
        }
    }
}
