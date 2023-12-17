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
        private readonly int Id;

        public Node ( World world )
        {
            _lookup = world.Lookup;
        }

    }
}
