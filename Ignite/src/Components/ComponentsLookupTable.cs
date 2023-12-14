using Ignite.Utils;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Ignite.Components
{
    public abstract class ComponentsLookupTable
    {
        protected ImmutableDictionary<Type, UniqueID> MessagesIndices { get; init; } = 
            new Dictionary<Type, UniqueID>().ToImmutableDictionary();

        public ImmutableHashSet<TypeUniqueID> RelativeComponents { get; protected init; } =
            ImmutableHashSet.Create(
                TypeUniqueID.GetOrCreateUniqueID<ITransformComponent>(),
                TypeUniqueID.GetOrCreateUniqueID<IPhysicComponent>());

        internal int TotalIndices => TypeUniqueID.RegisteredIDs + MessagesIndices.Count;

        /// <summary>
        /// Try get the id of a type component. If the type isn't registered, it'll create a unique id for the component type and return it.
        /// </summary>
        /// <param name="type">Type of the component</param>
        /// <returns>Unique Id of the component</returns>
        public TypeUniqueID GetId(Type type)
        {
            // Todo : add || typeof(IMessage).IsAssignableFrom(type)
            // when IMessage system is created
            Debug.Assert(typeof(IComponent).IsAssignableFrom(type));


            return TypeUniqueID.GetOrCreateUniqueID(type);
        }

        public bool IsRelative(Type type)
        {
            return RelativeComponents.Contains(TypeUniqueID.GetOrCreateUniqueID(type));
        }
    }
}
