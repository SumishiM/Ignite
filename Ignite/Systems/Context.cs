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


        private ImmutableDictionary<int, ImmutableArray<int>> _targets;


        private ComponentLookupTable _lookup;

        public Context(World world) 
        {
            _lookup = world.Lookup;
        }
    
        
    }
}
