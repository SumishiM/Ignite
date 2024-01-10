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
        internal ulong Id;

        private bool _isActive = true;
        public bool IsActive => _isActive;

        private bool _pendingDestroy = false;
        public bool IsDestroyed => _pendingDestroy;

        private bool _isPaused = false;
        public bool IsPaused => _isPaused;

        /// <summary>
        /// Whether the node will keep working during world pause or not
        /// </summary>
        public bool IgnorePause { get; private set; } = false;

        public World World { get; private set; }

        [System.Flags]
        public enum Flags : ulong
        {
            Empty = 0,
            Disabled = 0b1ul << 63,
            PendingDestroy = 0b1ul << 62,
        }

        public class UID
        {
            public ulong Id { get; internal set; }

            public void SetFlags(Node.Flags flags)
            {
                //Id = (Id & RestOfBitsMask) | ((ulong)flags << 48);
            }

            public void RemoveFlags(Node.Flags flags)
            {
                //Id ^= (ulong)flags;
            }

            public bool HasFlag(Node.Flags flags)
            {
                return (Id & (ulong)flags) == (ulong)flags; // Check if the flag is set using bitwise AND
            }

            public static ulong LastGeneratedId { get; private set; } = 0;

            private static ulong CurrentId = 0;
            private static ushort CurrentGenerationId = 0;

            public static ulong Next(Node.Flags flags = Flags.Empty)
            {
                ulong id = (ulong)flags;

                if (++CurrentId < UInt32.MaxValue)
                    id += CurrentId;
                else
                {
                    CurrentId = 0;
                    if (++CurrentGenerationId < UInt16.MaxValue)
                        id += CurrentGenerationId;
                    else
                        CurrentGenerationId = 0;
                }

                LastGeneratedId = id;
                return id;
            }

            public static implicit operator UID(ulong id) => new() { Id = id };
            public static implicit operator ulong(UID id) => id.Id;
        }

        public Node(World world)
        {
            World = world;
            _lookup = world.Lookup;

            Id = new UID();
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

            if ( _isPaused )
                Resume();
        }

        public virtual void Disable()
        {
            if (!_isActive) return;

            _isActive = false;
            OnDisabled?.Invoke(this);
        }

        /// <summary>
        /// If the Node is paused then it disables it 
        /// </summary>
        private void Pause()
        {
            if ( _isPaused ) return;
            _isPaused = true;
            OnPaused?.Invoke(this);
            Disable();
        }

        /// <summary>
        /// If the node is disabled then we dont resume it
        /// ? Might cause some issues later ?
        /// </summary>
        private void Resume ()
        {
            if( !_isPaused || !_isActive ) return;

            _isPaused = false;
            OnResumed?.Invoke(this);
            Enable();
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
