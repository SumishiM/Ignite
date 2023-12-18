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
        internal readonly int Id;

        private bool _isActive = true;
        public bool IsActive => _isActive;

        /// <summary>
        /// Whether the node will keep working during world pause or not
        /// </summary>
        public bool IgnorePause { get; private set; } = false;

        public Node(World world)
        {
            _lookup = world.Lookup;
            CheckIgnorePause();
            CheckRequiredComponents();
        }

        public virtual void Enabled()
        {
            OnEnabled?.Invoke(this);
        }

        public virtual void Disabled()
        {
            OnDisabled?.Invoke(this);
        }

        public void Destroy()
        {
            Dispose();
        }

        public void Dispose()
        {
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
