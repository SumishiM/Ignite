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
        public Node? Parent => _parent;


        private readonly Dictionary<int, Node> _children = new();
        internal Dictionary<int, Node> ChildrenIndex => _children;
        public ImmutableArray<Node> Children => _children.Values.ToImmutableArray();

        /// <summary>
        /// Set the parent of this node. If null, the parent will be the world root.
        /// </summary>
        public void SetParent(Node? parent)
        {
            // todo : set _parent a world.root if parent null
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
            foreach ((int id, Node child) in _children)
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
