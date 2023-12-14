using System.Collections.Immutable;
using System.Diagnostics;

namespace Ignite.Components
{
    public abstract class ComponentsLookupTable
    {
        private const int NextIndex = 8;

        protected ImmutableDictionary<Type, int> ComponentsIndices { get; init; } = new Dictionary<Type, int>()
        {
            { typeof(IComponent), IgniteComponentsTypes.Basic },
            { typeof(IModificationComponent), IgniteComponentsTypes.Modification },
            { typeof(IInteractiveComponent), IgniteComponentsTypes.Interactive },
            { typeof(IRenderingComponent), IgniteComponentsTypes.Rendering },
            { typeof(IBehavioralComponent), IgniteComponentsTypes.Behavioral },
            { typeof(ITransformComponent), IgniteComponentsTypes.Transform },
            { typeof(IPhysicComponent), IgniteComponentsTypes.Physic },
            { typeof(IAudioComponent), IgniteComponentsTypes.Audio }
        }.ToImmutableDictionary();

        protected ImmutableDictionary<Type, int> MessagesIndices { get; init; } = 
            new Dictionary<Type, int>().ToImmutableDictionary();

        protected Dictionary<Type, int> _untrackedComponents = [];

        public ImmutableHashSet<int> RelativeComponents { get; protected init; } =
            ImmutableHashSet.Create(IgniteComponentsTypes.Transform, IgniteComponentsTypes.Physic);

        private readonly Dictionary<Type, int> _untrackedIndices = [];
        private readonly HashSet<int> _untrackedRelativeComponents = [];
        private int? _nextUntrackedIndex;

        internal int TotalIndices => ComponentsIndices.Count + MessagesIndices.Count + _untrackedIndices.Count;

        /// <summary>
        /// Try get the id of a type component. If the type isn't registered, it'll create a unique id for the component type and return it.
        /// </summary>
        /// <param name="type">Type of the component</param>
        /// <returns>Unique Id of the component</returns>
        public int GetId(Type type)
        {
            // Todo : add || typeof(IMessage).IsAssignableFrom(type)
            // when IMessage system is created
            Debug.Assert(typeof(IComponent).IsAssignableFrom(type));

            int index;

            if (ComponentsIndices.TryGetValue(type, out index))
            {
                return index;
            }

            if (_untrackedComponents.TryGetValue(type, out index))
            {
                return index;
            }

            return AddUntrackedIndexForComponent(type);
        }

        public bool IsRelative(int id)
        {
            return RelativeComponents.Contains(id);
        }

        /// <summary>
        /// Register untracked Component and generate a unique Id associated to the component type
        /// </summary>
        /// <param name="type">Type of the component</param>
        /// <returns>Unique Id of the component</returns>
        public int AddUntrackedIndexForComponent(Type type)
        {
            _nextUntrackedIndex ??= ComponentsIndices.Count + MessagesIndices.Count;

            int? id = _nextUntrackedIndex++;

            _untrackedIndices.Add(type, id.Value);

            if (typeof(IParentRelativeComponent).IsAssignableFrom(type))
            {
                _untrackedRelativeComponents.Add(id.Value);
            }

            return id.Value;
        }
    }
}
