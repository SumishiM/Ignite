using Ignite.Components;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Ignite
{
    public abstract class Node
    {
        public Dictionary<int, IComponent> Components { get; protected set; } =
            new Dictionary<int, IComponent>();

        private readonly int Id;
        private readonly ComponentLookupTable _lookup;

        public Node ( World world )
        {
            _lookup = world.Lookup;
        }

        public Node ( World world, params IComponent[] components )
            : this(world)
        {
            foreach ( var component in components )
            {
                Components.Add(_lookup[component.GetType()], component);
            }
        }

        /*
        public Node(World world, params Type[] components)
            : this(world)
        {
            foreach (var component in components)
            {
                //Component factory to create the components
                //Components.Add(_lookup[component], component);
            }
        }
        */

        public bool HasComponent<T> () where T : class, IComponent
            => HasComponent(_lookup[typeof(T)]);

        public bool HasComponent ( Type type )
            => HasComponent(_lookup[type]);

        internal bool HasComponent ( int index )
        {
            return Components.ContainsKey(index);
        }

        internal bool HasComponent ( IComponent component)
        {
            return Components.ContainsKey(_lookup[component]);
        }

        public bool TryGetComponent<T> ( [NotNullWhen(true)] out T? component )
            where T : class, IComponent
        {
            if ( TryGetComponent(typeof(T), out IComponent? result) )
            {
                component = (T)result;
                return true;
            }

            component = null;
            return false;
        }

        public bool TryGetComponent ( Type type, [NotNullWhen(true)] out IComponent? component )
        {
            Debug.Assert(typeof(IComponent).IsAssignableFrom(type),
                $"Why are we trying to get and object that isn't a component!?");

            if ( Components.TryGetValue(_lookup[type], out IComponent? value) )
            {
                component = value;
                return true;
            }

            component = null;
            return false;
        }

        public T GetComponent<T> () where T : class, IComponent
        {
            Debug.Assert(HasComponent<T>(), $"This node doesn't contain this component");
            return (T)Components[_lookup[typeof(T)]];
        }

        public IComponent GetComponent ( Type type )
        {
            Debug.Assert(typeof(IComponent).IsAssignableFrom(type),
                $"Why are we trying to get and object that isn't a component!?");
            Debug.Assert(HasComponent(type), $"This node doesn't contain this component");
            return Components[_lookup[type]];
        }

        internal int GetComponentIndex<T> ()
        {
            return _lookup[typeof(T)];
        }

        internal int GetComponentIndex ( Type type )
        {
            return _lookup[type];
        }

        public Node AddComponent<T> () where T : class, IComponent, new()
            => AddComponent(new T());

        public Node AddComponent ( Type type )
        {
            Debug.Assert(typeof(IComponent).IsAssignableFrom(type),
                $"Why are we trying to add/replace a component with a type that isn't a component ?");

            return AddComponent((IComponent)Activator.CreateInstance(type)!);
        }

        public Node AddComponent<T> ( T component ) where T : class, IComponent
        {
            Debug.Assert(!HasComponent(component),
                $"Why are we adding a component to a node already having one of the same type ? Try AddOrReplaceComponent instead.");
            
            Components[_lookup[component.GetType()]] = component;
            return this;
        }

        public Node AddOrReplaceComponent<T> () where T : class, IComponent, new()
            => AddOrReplaceComponent(new T());

        public Node AddOrReplaceComponent ( Type type )
        {
            Debug.Assert(typeof(IComponent).IsAssignableFrom(type),
                $"Why are we trying to add/replace a component with a type that isn't a component ?");

            return AddOrReplaceComponent((IComponent)Activator.CreateInstance(type)!);
        }

        public Node AddOrReplaceComponent<T> ( T component ) where T : class, IComponent
        {
            Components[_lookup[component.GetType()]] = component;
            return this;
        }

        public Node RemoveComponent<T>()
        {
            Components.Remove(_lookup[typeof(T)]);
            return this;
        }

        public Node RemoveComponent(Type type)
        {
            Components.Remove(_lookup[type]);
            return this;
        }

    }
}
