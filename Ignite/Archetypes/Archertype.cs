using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ignite.Archetypes
{

    public struct Archetype
    {
        public int Id;
        public List<IComponent[]> Components;
        public Dictionary<int, ArchetypeEdge> Edges;
    }
    public struct ArchetypeEdge
    {
        public Archetype Add;
        public Archetype Remove;
    }

    public struct ArchetypeRecord
    {
        public Archetype Archetype;
        public int Rows;
    }

    public struct ArchetypeMap
    {
        Dictionary<int, ArchetypeRecord> Map;
        public Archetype Archetype;
        public int Rows;
    }

    public struct temp
    {
        /// <summary>
        /// Key = Component Id, Value = Hashset(Archetype id)
        /// </summary>
        Dictionary<int, HashSet<int>> componentIndex; // archetype set
    }
}
