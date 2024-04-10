using System.Collections.Immutable;

namespace Ignite
{
    /** 
     * This part of the Node class is handle the nodes hierarchy
    **/
    public partial class Node
    {
        public event Action<Node, Node?>? OnParentChanged;
        public event Action<Node, Node>? OnChildAdded;
        public event Action<Node, Node, bool>? OnChildRemoved;

        protected Node? _parent;

        /// <summary>
        /// Parent node in the hierarchy
        /// </summary>
        public Node? Parent => _parent;
        

        /// <summary>
        /// Every nodes set as children of this node
        /// </summary>
        private readonly Dictionary<ulong, Node> _children = new();

        /// <summary>
        /// Childen Node id to childen Node object
        /// </summary>
        internal Dictionary<ulong, Node> ChildrenIndex => _children;

        /// <summary>
        /// Every direct children of the node
        /// </summary>
        public ImmutableArray<Node> Children => _children.Values.ToImmutableArray();

        /// <summary>
        /// Set the parent of this node. If null, the parent will be the world root.
        /// </summary>
        public void SetParent(Node? parent)
        {
            if (_parent != null && _parent != parent)
                _parent.RemoveChild(this);

            if (parent == null)
                parent = World.Root;
            else
                _parent = parent;

            OnParentChanged?.Invoke(this, parent);
        }

        /// <summary>
        /// Add a node as child of this node
        /// </summary>
        public Node AddChild(Node child)
        {
            _children[child.Id] = child;
            OnChildAdded?.Invoke(this, child);
            child.SetParent(this);
            return this;
        }

        /// <summary>
        /// Add a list nodes as children of this node
        /// </summary>
        public Node AddChildren(IEnumerable<Node> children)
        {
            foreach (var child in children)
            {
                AddChild(child);
            }
            return this;
        }

        /// <summary>
        /// Remove a child node
        /// </summary>
        /// <param name="child">Node to remove</param>
        public Node RemoveChild(Node child)
        {
            if (_children.Remove(child.Id))
            {
                OnChildRemoved?.Invoke(this, child, false);
                child.SetParent(null);
            }

            return this;
        }

        /// <summary>
        /// Destroy every children recurcively 
        /// </summary>
        internal void DestroyChildren()
        {
            foreach ((ulong id, Node child) in _children)
            {
                if (_children.Remove(id))
                {
                    OnChildRemoved?.Invoke(this, child, true);
                    child.Destroy();
                }
            }
        }
    }
}
