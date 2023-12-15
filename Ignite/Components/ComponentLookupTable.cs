using System.Collections.Immutable;
using System.Diagnostics;

namespace Ignite.Components
{
    public class ComponentLookupTable
    {
        private readonly ImmutableDictionary<Type, int> _componentsIndex = 
            new Dictionary<Type, int>().ToImmutableDictionary();

        public int this[Type type] => GetIndex(type);

        public int GetIndex<T>() where T : class, IComponent
            => GetIndex(typeof(T));

        public int GetIndex(Type type)
        {
            Debug.Assert(typeof(IComponent).IsAssignableFrom(type),
                $"Why are we trying to get a component index for a not component type ?");

            Debug.Assert(_componentsIndex.ContainsKey(type),
                $"Why are we trying to get a component id for a non-registed component?\n" +
                $"Try using {nameof(Ignite.Components.ComponentLookupTable.GetOrCreateIndex)} instead!");

            return _componentsIndex[type];
        }

        public int GetOrCreateIndex<T>() where T : class, IComponent
            => GetOrCreateIndex(typeof(T));

        public int GetOrCreateIndex(Type type)
        {
            Debug.Assert(typeof(IComponent).IsAssignableFrom(type),
                $"Why are we trying to get a component index for a not component type ?");

            if (_componentsIndex.TryGetValue(type, out var index))
                return index;

            index = _componentsIndex.Count;
            _componentsIndex.Add(type, index);
            return index;
        }
    }
}
