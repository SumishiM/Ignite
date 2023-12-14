using Ignite.Components;
using Ignite.Systems;
using System.Diagnostics;

namespace Ignite.Utils
{
    internal class InvalidType { }

    public class TypeUniqueID : IEquatable<TypeUniqueID>
    {
        private readonly int _id;
        private readonly Type _type;

        private const int IgniteReservedIDs = 25;
        private static int _lastAttributedID = IgniteReservedIDs;

        private static readonly Dictionary<Type, int> _typesIDs = new()
        {
            { typeof(IComponent), 1 },
            { typeof(IModificationComponent), 2 },
            { typeof(IInteractiveComponent), 3 },
            { typeof(IRenderingComponent), 4 },
            { typeof(IBehavioralComponent), 5 },
            { typeof(ITransformComponent), 6 },
            { typeof(IPhysicComponent), 7 },
            { typeof(IAudioComponent), 8 }
        };

        public static int RegisteredIDs => _typesIDs.Count;
        public static TypeUniqueID Empty = GetOrCreateUniqueID<Ignite.Utils.InvalidType>();

        public TypeUniqueID(Type type)
        {
            _type = type;
            _id = GetOrCreateID(type);
        }

        public static TypeUniqueID Get(Type type)
        {
            Debug.Assert(Exist(type));
            return new(type);
        }

        public static TypeUniqueID Get<T>()
        {
            Debug.Assert(Exist(typeof(T)));
            return new(typeof(T));
        }

        public static bool Exist(Type type)
            => _typesIDs.ContainsKey(type);
        
        

        /// <summary>
        /// Create a <see cref="Ignite.Utils.TypeUniqueID"/> from a <see cref="Type"/> <typeparamref name="T"/>
        /// with the associated id
        /// </summary>
        /// <returns><see cref="Ignite.Utils.TypeUniqueID"/> of type <typeparamref name="T"/></returns>
        public static TypeUniqueID GetOrCreateUniqueID<T>() where T : notnull
            => GetOrCreateUniqueID(typeof(T));

        /// <summary>
        /// Create a <see cref="Ignite.Utils.TypeUniqueID"/> from a <see cref="Type"/> 
        /// with the associated id
        /// </summary>
        /// <returns><see cref="Ignite.Utils.TypeUniqueID"/> from the given <see cref="Type"/></returns>
        public static TypeUniqueID GetOrCreateUniqueID(Type type)
        {
            if(typeof(IComponent).IsAssignableFrom(type)
                || typeof(ISystem).IsAssignableFrom(type))
            {
                Debug.Fail($"Recieved a type {type.Name} which is not valid.");
                return TypeUniqueID.Empty;
            }

            if (_typesIDs.ContainsKey(type))
                return new(type);

            Register(type);
            return new(type);
        }

        private static int GetOrCreateID(Type type)
        {
            if (_typesIDs.TryGetValue(type, out int value))
                return value;

            _typesIDs[type] = ++_lastAttributedID;
            return _lastAttributedID;
        }

        /// <summary>
        /// Register a <see cref="Type"/> <typeparamref name="T"/> in the components ids table.
        /// </summary>
        /// <returns>Whether the type has been added or not.</returns>
        public static bool Register<T>() where T : notnull
            => Register(typeof(T), GetOrCreateID(typeof(T)));

        /// <summary>
        /// Register a <see cref="Type"/> in the components ids table.
        /// </summary>
        /// <returns>Whether the type has been added or not.</returns>
        public static bool Register(Type type)
            => Register(type, GetOrCreateID(type));


        /// <summary>
        /// Register a <see cref="Type"/> <typeparamref name="T"/> in the components ids table.
        /// </summary>
        /// <returns>Whether the type has been added or not.</returns>
        internal static bool Register<T>(int id) where T : notnull
            => Register(typeof(T), id);

        /// <summary>
        /// Register a <see cref="Type"/> in the components ids table.
        /// </summary>
        /// <returns>Whether the type has been added or not.</returns>
        internal static bool Register(Type type, int id)
            => _typesIDs.TryAdd(type, id);

        public bool Equals(TypeUniqueID? other)
        {
            return other?._type == _type;
        }

        public override bool Equals(object obj)
        {
            if (obj is not TypeUniqueID)
                return false;
            return Equals(obj as TypeUniqueID);
        }

        public override int GetHashCode()
        {
            return _id.GetHashCode() + _type.GetHashCode();
        }
    }
}
