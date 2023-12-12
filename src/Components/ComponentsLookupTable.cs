using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ignite.Components
{
    public abstract class ComponentsLookupTable
    {
            
        protected ImmutableDictionary<Type, int> ComponentsIndices { get; init; } = new Dictionary<Type, int>()
        {
            { typeof(IComponent), 0}
        }.ToImmutableDictionary();

    }
}
