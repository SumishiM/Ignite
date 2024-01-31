namespace Ignite
{
    /// <summary>
    /// Base class for Ignite entities, act as a <see cref="Ignite.Components.IComponent"/>s container
    /// Every world objects in Ignite needs to inherit from this class in order to work.
    /// </summary>
    public partial class Node : IDisposable
    {
        public event Action<Node>? OnPaused;
        public event Action<Node>? OnResumed;
        public event Action<Node>? OnEnabled;
        public event Action<Node>? OnDisabled;
        public event Action<Node>? OnDestroyed;

        /// <summary>
        /// Unique Id for the node in the world
        /// </summary>
        public ulong Id;

        private bool _isEnabled = true;
        public bool IsEnabled => _isEnabled;

        private bool _pendingDestroy = false;
        public bool IsDestroyed => _pendingDestroy;

        /// <summary>
        /// Whether the node will keep working during world pause or not
        /// </summary>
        public bool IgnorePause { get; private set; } = false;

        public World World { get; private set; }
        public string Name { get; set; } = "Unnamed Node";

        [System.Flags]
        public enum Flags : ulong
        {
            Empty = 0,
            Disabled = 0b1ul << 63,
            PendingDestroy = 0b1ul << 62,
        }

        public Node(World world, string name = "Unnamed Node")
        {
            World = world;
            _lookup = world.Lookup;
            Name = name;

            Id = world._UIDGenerator.Next();
        }

        public virtual void Enable()
        {
            if (_isEnabled) return;

            _isEnabled = true;
            OnEnabled?.Invoke(this);
        }

        public virtual void Disable()
        {
            if (!_isEnabled) return;

            _isEnabled = false;
            OnDisabled?.Invoke(this);
        }

        public virtual void Destroy()
        {
            if (_pendingDestroy)
                return;

            _pendingDestroy = true;

            Disable();

            World.TagNodeForDestroy(this);
        }

        public void Dispose()
        {
            RemoveAllComponents();
            DestroyChildren();

            _parent = null;
            OnEnabled = null;
            OnDisabled = null;
            OnDestroyed = null;
            OnChildAdded = null;
            OnChildRemoved = null;
            OnParentChanged = null;
            OnComponentAdded = null;
            OnComponentRemoved = null;
            OnComponentReplaced = null;

            OnDestroyed?.Invoke(this);

            GC.SuppressFinalize(this);
        }
    }
}
