using System.Collections.Immutable;
using System.Diagnostics;
using Ignite.Components;
using Ignite.Systems;

namespace Ignite
{
    public partial class World
    {
        // Node / hierarchy
        /// <summary>
        /// Root of the world for nodes hierarchy. Every orphan nodes should be rattached to the root.
        /// </summary>
        public Node Root { get; private set; }

        /// <summary>
        /// Every nodes in the world
        /// </summary>
        public readonly Dictionary<ulong, Node> Nodes;

        /// <summary>
        /// Nodes index waiting to be destroyed
        /// </summary>
        private readonly HashSet<ulong> _pendingDestroyNodes = [];

        /// <summary>
        /// Add a node to the world
        /// </summary>
        public Node AddNode(Node.Builder builder)
        {
            return builder.ToNode();
        }

        /// <summary>
        /// Add a node to the world
        /// </summary>
        public Node AddNode(string name = "Unnamed Node") => AddNode(name, Array.Empty<IComponent>());

        /// <summary>
        /// Add a node to the world with components
        /// </summary>
        public Node AddNode(string name = "Unnamed Node", params IComponent[] components)
        {
            var builder = Node.CreateBuilder(this, name);

            builder.AddComponents(components);

            return builder.ToNode();
        }

        /// <summary>
        /// Add a node to the world with empty components
        /// </summary>
        public Node AddNode(string name = "Unnamed Node", params Type[] components)
        {
            var builder = Node.CreateBuilder(this, name);

            builder.AddComponents(components);

            return builder.ToNode();
        }

        /// <summary>
        /// Fetch nodes in teh world with a given set of components.
        /// Todo : implement archetypes to maake this process faster
        /// </summary>
        public ImmutableArray<Node> GetNodesWith(params Type[] components)
        {
            return GetNodesWith(Context.AccessFilter.AllOf, components);
        }

        /// <summary>
        /// Fetch nodes in teh world with a given set of components.
        /// Todo : implement archetypes to maake this process faster
        /// </summary>
        public ImmutableArray<Node> GetNodesWith(Context.AccessFilter filter, params Type[] components)
        {
            int id = GetOrCreateContext(filter, components.Select(t => Lookup.GetIndex(t)).ToArray());
            return _contexts[id].Nodes;
        }

        /// <summary>
        /// Register a node in the world if it id is unique
        /// </summary>
        internal void RegisterNode(Node node)
        {
            if (Root == null) return;
            Debug.Assert(Nodes.TryAdd(node.Id, node),
                $"A node with this Id ({node.Id}) is already registered in the world !");
            
            node.World = this;
            
            if (node.Parent == null)
                Root.AddChild(node);

            // O(n) loop, try optimize later maybe probably
            foreach ((int _, Context c) in _contexts)
            {
                c.TryRegisterNode(node);
            }
        }

        /// <summary>
        /// Mark a node to be destroyed 
        /// </summary>
        internal void TagNodeForDestroy(Node node)
        {
            // update tags on node id
            _pendingDestroyNodes.Add(node.Id);
        }

        /// <summary>
        /// Destroy all nodes marked to be destroyed
        /// </summary>
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
