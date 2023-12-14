using Ignite.Contexts;
using Ignite.Entities;
using Ignite.Systems;
using System.ComponentModel;

namespace Ignite.Utils
{
    /// <summary>
    /// Represent a unique id for a registered <see cref="Type"/>.
    /// Ignite types are registered by default and other types 
    /// can be addes with <see cref="Ignite.Utils.UniqueID.RegisterType(Type)"/>.
    /// Ids from deleted objects will be reattributed automatically.
    /// </summary>
    public sealed class UniqueID : IDisposable, IEquatable<UniqueID>
    {
        private readonly int _id = 0;
        private readonly Type _type;

        /// <summary>
        /// Lookup table for unique ids registering types 
        /// </summary>
        private static readonly Dictionary<Type, int> _lookup = new()
        {
            { typeof(IComponent), 0 },
            { typeof(ISystem), 0 },
            { typeof(Context), 0 },
            { typeof(Entity), 0 },
        };

        private static readonly Dictionary<Type, Queue<int>> _prendingReattributionIds = new()
        {
            { typeof(IComponent), new() },
            { typeof(ISystem), new() },
            { typeof(Context), new() },
            { typeof(Entity), new() },
        };

        /// <summary>
        /// Create a unique id for the given <see cref="Type"/>.
        /// </summary>
        /// <param name="type"><see cref="Type"/> of the object requesting a unique id.</param>
        public UniqueID(Type type)
        {
            _type = type;
            _id = NextId(type);
        }

        public void Dispose()
        {
            if (!_prendingReattributionIds.ContainsKey(_type))
                _prendingReattributionIds.Add(_type, new());
            _prendingReattributionIds[_type].Enqueue(_id);
        }

        /// <summary>
        /// Return the next id for a given <see cref="Type"/>.
        /// </summary>
        /// <param name="type"><see cref="Type"/> of the object requesting a unique id.</param>
        /// <returns>Unique id as int</returns>
        private static int NextId(Type type)
        {
            foreach ((Type t, int _) in _lookup)
            {
                if (t.IsAssignableFrom(type))
                {
                    if (_prendingReattributionIds[t].TryDequeue(out int result))
                        return result;
                    return ++_lookup[t];
                }
                
                // if the object is a parent class of t we move the ids from t to this one
                if(t.IsAssignableTo(type))
                {
                    _lookup[type] = _lookup[t];
                    _lookup.Remove(t);

                    if (_prendingReattributionIds[t].TryDequeue(out int result))
                        return result;
                    return ++_lookup[type];
                }
            }

            return _lookup[type] = 1;
        }

        /// <summary>
        /// Add a type for unique ids 
        /// </summary>
        public static void RegisterType(Type type)
        {
            _lookup.TryAdd(type, 0);
        }

        /// <summary>
        /// Give the next <see cref="Ignite.Utils.UniqueID"/> of type <typeparamref name="T"/>
        /// </summary>
        public static UniqueID Next<T>()
            => new(typeof(T));

        /// <summary>
        /// Give the next <see cref="Ignite.Utils.UniqueID"/> of type <paramref name="type"/>
        /// </summary>
        public static UniqueID Next(Type type)
            => new(type);

        public bool Equals(UniqueID? other) => _id == other?._id;
        public override bool Equals(object? obj) => Equals(obj as UniqueID);
        public override int GetHashCode() => _id.GetHashCode();


        public static implicit operator int(UniqueID id) => id._id;
    }
}
