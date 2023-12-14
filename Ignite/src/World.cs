using Ignite.Components;
using Ignite.Contexts;
using Ignite.Entities;
using Ignite.Utils;
using Ignite.Systems;

namespace Ignite
{
    public class World
    {
        public ComponentsLookupTable Lookup { get; private set; }

        
        private HashSet<TypeUniqueID> _IgnorePauseSystems;
        private HashSet<TypeUniqueID> _PauseSystems;

        private Dictionary<TypeUniqueID, ISystem> _systems;
        private Dictionary<UniqueID, Entity> _entities;
        private Dictionary<TypeUniqueID, Context> _contexts;

        private HashSet<UniqueID> _pendingDestroyEntities;
        private HashSet<TypeUniqueID> _pendingDestroySystems;

        public int EntityCount => _entities.Count;
        public int SystemCount => _systems.Count;
        public int ContextCount => _contexts.Count;

        public World()
        {
            Lookup = FindComponentLookupTable();
        }

        private ComponentsLookupTable FindComponentLookupTable()
        {
            return new object() as ComponentsLookupTable;
        }
    }
}
