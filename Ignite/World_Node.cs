using System.Collections.Immutable;
using System.Diagnostics;
using Ignite.Components;
using Ignite.Systems;

namespace Ignite
{
    public partial class World
    {
        // Node / hierarchy
        public Node Root { get; private set; }
        public readonly Dictionary<ulong, Node> Nodes;
        private readonly HashSet<ulong> _pendingDestroyNodes = [];

        public Node AddNode() => AddNode(Array.Empty<IComponent>());

        public Node AddNode(params IComponent[] components)
        {
            var builder = Node.CreateBuilder(this);

            builder.AddComponents(components);

            return builder.ToNode();
        }

        public Node AddNode(params Type[] components)
        {
            var builder = Node.CreateBuilder(this);

            builder.AddComponents(components);

            return builder.ToNode();
        }

        public ImmutableArray<Node> GetNodesWith(params int[] components)
        {

            Context context = GetOrCreateContext(components);
            return context.Nodes;
        }

        public ImmutableArray<Node> GetNodesWith(params Type[] components)
        {

            Context context = GetOrCreateContext(components);
            return context.Nodes;
        }

        internal void RegisterNode(Node node)
        {
            Debug.Assert(!Nodes.TryAdd(node.Id, node),
                $"A node with this Id ({node.Id}) is already registered in the world !");

            if (node.Parent == null)
                node.SetParent(Root);

            // O(n) loop, try optimize later maybe probably
            foreach ((int _, Context c) in _contexts)
            {
                c.TryRegisterNode(node);
            }
        }

        internal void TagNodeForDestroy(Node node)
        {
            // update tags on node id
            _pendingDestroyNodes.Add(node.Id);
        }

        private void DestroyPendingNodes()
        {
            ImmutableArray<ulong> pendingDestroy = _pendingDestroyNodes.ToImmutableArray();
            _pendingDestroyNodes.Clear();

            foreach (var id in pendingDestroy)
            {
                Node node = Nodes[id];
                Nodes.Remove(id);
                node.Dispose();
            }
        }
    }
}
