using Ignite.Components;
using Ignite.Systems;
using System.Diagnostics;
using System.Security.Principal;

namespace Ignite
{
    /// <summary>
    /// <para>
    /// Whats left ?
    /// Register nodes automatically
    /// Get lookup table
    /// </para>
    /// </summary>
    public class World
    {
        public Action<World>? OnDestroyed;
        public Action? OnPaused;
        public Action? OnResumed;

        public ComponentLookupTable Lookup { get; set; }

        public Node Root { get; private set; }
        public Dictionary<ulong, Node> Nodes { get; set; }


        public SortedList<int, (IStartSystem system, int context)> _cachedStartSystem;
        public SortedList<int, (IExitSystem system, int context)> _cachedExitSystem;
        public SortedList<int, (IUpdateSystem system, int context)> _cachedUpdateSystem;
        public SortedList<int, (IRenderSystem system, int context)> _cachedRenderSystem;


        private bool _destroying = false;

        public World()
        {
            Root = Node.CreateBuilder(this).ToNode();
        }

        internal void RegisterNode(Node node)
        {
            Debug.Assert(!Nodes.TryAdd(node.Id, node),
                $"A node with this Id ({node.Id}) is already registered in the world !");

            // O(n) loop, try optimize later
            foreach ((int _, Context c) in Contexts)
            {
                c.TryRegisterNode(node);
            }
        }

        internal void UnregisterNode(Node node)
        {
            Nodes.Remove(node.Id);
        }

        public void RegisterSystem(ISystem system)
        {
            Debug.Assert(!Systems.ContainsValue(system),
                $"Why are we trying to add the same system more than once ?");

            Systems.TryAdd(system);
        }

        public void Pause()
        {
            OnPaused?.Invoke();
        }

        public void Resume()
        {
            OnResumed?.Invoke();
        }

        public void Destroy()
        {
            if (_destroying)
                return;
            _destroying = true;

            Root.Destroy();
            OnDestroyed?.Invoke(this);
        }
    }
}
