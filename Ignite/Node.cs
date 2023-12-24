using Ignite.Utils;

namespace Ignite
{
    /// <summary>
    /// Base class for Ignite entities, act as a container for <see cref="Ignite.Components.IComponent"/>s
    /// Every world objects in Ignite needs to inherit from this class in order to work.
    /// </summary>
    public partial class Node : IDisposable
    {
        public event Action<Node>? OnEnabled;
        public event Action<Node>? OnDisabled;
        public event Action<Node>? OnDestroyed;

        /// <summary>
        /// Unique Id for the node in the world
        /// </summary>
        internal NodeId Id;

        private bool _isActive = true;
        public bool IsActive => _isActive;

        private bool _pendingDestroy = false;

        /// <summary>
        /// Whether the node will keep working during world pause or not
        /// </summary>
        public bool IgnorePause { get; private set; } = false;

        public World World { get; private set; }

        [System.Flags]
        public enum Flags : ulong
        {
            Disabled = 1ul << 63,
            PendingDestroy = 1ul << 62,

        }

        public Node(World world)
        {
            World = world;
            _lookup = world.Lookup;

            Id = new NodeId();
            world.RegisterNode(this);
            
            CheckIgnorePause();
            CheckRequiredComponents();

            OnDestroyed += world.UnregisterNode;
        }

        public virtual void Enable()
        {
            if (_isActive) return;

            _isActive = true;
            OnEnabled?.Invoke(this);
        }

        public virtual void Disable()
        {
            if (!_isActive) return;

            _isActive = false;
            OnDisabled?.Invoke(this);
        }

        public virtual void Destroy()
        {
            Dispose();
        }

        public void Dispose()
        {
            if(_pendingDestroy)
                return; 
            
            _pendingDestroy = true;

            Disable();

            RemoveAllComponents();
            DestroyChildren();

            _parent = null;
            OnDestroyed?.Invoke(this);

            OnEnabled = null;
            OnDisabled = null;
            OnDestroyed = null;
            OnChildAdded = null;
            OnChildRemoved = null;
            OnParentChanged = null;
            OnComponentAdded = null;
            OnComponentRemoved = null;
            OnComponentReplaced = null;

            GC.SuppressFinalize(this);
        }
    }
}
