using Ignite.Components;
using Ignite.Utils;

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
        public Action<World>? OnPaused;
        public Action<World>? OnResumed;

        public ComponentLookupTable Lookup { get; set; }

        public Node Root { get; private set; }
        public Dictionary<NodeId, Node> Nodes { get; set; }

        private bool _destroying = false;

        public World ()
        {
            Root = Node.CreateBuilder(this).ToNode();
        }

        internal void RegisterNode(Node node)
        {
            Nodes.TryAdd(node.Id, node);
        }

        internal void UnregisterNode(Node node)
        {
            Nodes.Remove(node.Id);
        }

        public void Pause()
        {
            OnPaused?.Invoke(this);
        }

        public void Resume()
        {
            OnResumed?.Invoke(this);
        }

        public void Destroy()
        {
            if ( _destroying )
                return;
            _destroying = true;

            Root.Destroy();
            OnDestroyed?.Invoke(this);
        }
    }
}
