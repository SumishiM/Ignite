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

        public Node AddNode(Node.Builder builder)
        {
            return builder.ToNode();
        }

        public Node AddNode(string name = "Unnamed Node") => AddNode(name, Array.Empty<IComponent>());

        public Node AddNode(string name = "Unnamed Node", params IComponent[] components)
        {
            var builder = Node.CreateBuilder(this, name);

            builder.AddComponents(components);

            return builder.ToNode();
        }

        public Node AddNode(string name = "Unnamed Node", params Type[] components)
        {
            var builder = Node.CreateBuilder(this, name);

            builder.AddComponents(components);

            return builder.ToNode();
        }

        public ImmutableArray<Node> GetNodesWith(params Type[] components)
        {
            return GetNodesWith(Context.AccessFilter.AllOf, components);
        }

        public ImmutableArray<Node> GetNodesWith(Context.AccessFilter filter, params Type[] components)
        {
            int id = GetOrCreateContext(filter, components.Select(t => Lookup.GetIndex(t)).ToArray());
            return _contexts[id].Nodes;
        }

        internal void RegisterNode(Node node)
        {
            if (Root == null) return;
            Debug.Assert(Nodes.TryAdd(node.Id, node),
                $"A node with this Id ({node.Id}) is already registered in the world !");

            if (node.Parent == null)
                Root.AddChild(node);

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
