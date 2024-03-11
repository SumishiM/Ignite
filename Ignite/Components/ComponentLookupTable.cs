using Ignite.Attributes;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Ignite.Components
{
    public class ComponentLookupTable
    {
        protected ImmutableDictionary<Type, int> _componentsIndex =
            new Dictionary<Type, int>() { }.ToImmutableDictionary();

        private ImmutableDictionary<int, Type>? _componentsTypes = null;
        protected ImmutableDictionary<int, Type> ComponentsTypes
        {
            get
            {
                _componentsTypes ??= _componentsIndex.ToImmutableDictionary(kvp => kvp.Value, kvp => kvp.Key);
                return _componentsTypes;
            }
        }

        internal ImmutableDictionary<int, ImmutableHashSet<Type>> RequiredComponentsLookup =
            new Dictionary<int, ImmutableHashSet<Type>>() { }.ToImmutableDictionary();

        public static int NextLookupId = 25;

        /// <summary>
        /// Create a lookup for every required components for each components
        /// </summary>
        internal void CheckRequirements()
        {
            var builder = ImmutableDictionary.CreateBuilder<int, ImmutableHashSet<Type>>();

            foreach (var component in _componentsIndex.Values)
            {
                if (_componentsIndex.FirstOrDefault(kvp => kvp.Value == component).Key is Type type)
                {
                    RequireComponentAttribute[] requires = (RequireComponentAttribute[])type
                        .GetCustomAttributes(typeof(RequireComponentAttribute), true);

                    foreach (var require in requires)
                    {
                        var indices = ImmutableHashSet.CreateBuilder<Type>();

                        foreach (Type componentType in require.Types)
                        {
                            indices.Add(componentType);
                        }

                        builder.Add(component, indices.ToImmutableHashSet());
                    }
                }
            }

            RequiredComponentsLookup = builder.ToImmutableDictionary();
        }

        /// <summary>
        /// Get the index of the component of <see cref="Type"/> <paramref name="type"/>.
        /// See <see cref="Ignite.Components.ComponentLookupTable.GetIndex(Type)"/>
        /// </summary>
        /// <param name="type"><see cref="Type"/> of the component we want the index of</param>
        /// <returns>Index of the component</returns>
        public int this[Type type] => GetOrCreateIndex(type);

        /// <summary>
        /// Get the index of the <paramref name="component"/>.
        /// See <see cref="Ignite.Components.ComponentLookupTable.GetIndex(Type)"/>
        /// </summary>
        /// <returns>Index of the <paramref name="component"/></returns>
        public int this[IComponent component] => GetOrCreateIndex(component.GetType());

        /// <summary>
        /// Get the index of the component of <see cref="Type"/> <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T"><see cref="Type"/> of the component we want the index of</typeparam>
        /// <returns>Index of the component</returns>
        public int GetIndex<T>() where T : class, IComponent
            => GetIndex(typeof(T));

        /// <summary>
        /// Get the index of the component of <see cref="Type"/> <paramref name="type"/>
        /// </summary>
        /// <param name="type"><see cref="Type"/> of the component we want the index of</param>
        /// <returns>Index of the component</returns>
        public int GetIndex(Type type)
        {
            Debug.Assert(typeof(IComponent).IsAssignableFrom(type),
                $"Why are we trying to get a component index for a not component type ?");

            Debug.Assert(_componentsIndex.ContainsKey(type),
                $"Why are we trying to get a component id for a non-registed component ?\n" +
                $"Try using {nameof(Ignite.Components.ComponentLookupTable.GetOrCreateIndex)} instead !");

            return _componentsIndex[type];
        }

        /// <summary>
        /// Get the <see cref="Type"/> of a component from it's index
        /// </summary>
        /// <param name="index">Index of the <see cref="IComponent"/></param>
        /// <returns><see cref="Type"/> of the <see cref="IComponent"/></returns>
        public Type GetTypeFromIndex(int index)
            => ComponentsTypes[index];

        /// <summary>
        /// Get the index of the component of <see cref="Type"/> <typeparamref name="T"/>
        /// or create an index for it
        /// </summary>
        /// <typeparam name="T"><see cref="Type"/> of the component we wand the index of</typeparam>
        /// <returns>Index of the component</returns>
        public int GetOrCreateIndex<T>() where T : class, IComponent
            => GetOrCreateIndex(typeof(T));


        /// <summary>
        /// Get the index of the component of <see cref="Type"/> <paramref name="type"/>
        /// or create an index for it
        /// </summary>
        /// <param name="type"><see cref="Type"/> of the component we wand the index of</param>
        /// <returns>Index of the component</returns>
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
