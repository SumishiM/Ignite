using Ignite.Components;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Ignite
{
    /// <summary>
    /// Base class for Ignite entities, act as a container for <see cref="Ignite.Components.IComponent"/>s
    /// Every world objects in Ignite needs to inherit from this class in order to work.
    /// </summary>
    public partial class Node
    {
        public event EventHandler OnEnabled;
        public event EventHandler OnDisabled;

        /// <summary>
        /// Unique Id for the node in the world
        /// </summary>
        private readonly int Id;

        private bool _isActive = true;
        public bool IsActive => _isActive;

        public Node(World world)
        {
            _lookup = world.Lookup;
        }

        public virtual void Enabled()
        {
            OnEnabled?.Invoke(this, EventArgs.Empty);
        }

        public virtual void Disabled()
        {
            OnDisabled?.Invoke(this, EventArgs.Empty);
        }
    }
}
