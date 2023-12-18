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


        private readonly Dictionary<int,  Node> _children = new();
        internal Dictionary<int, Node> ChildrenIndex => _children;
        public ImmutableArray<Node> Children => _children.Values.ToImmutableArray();

        public void SetParent(Node? parent)
        {
            _parent = parent;
            OnParentChanged?.Invoke(this, parent);
        }

        public Node AddChild(Node child)
        {
            _children[child.Id] = child;
            OnChildAdded?.Invoke(this, child);
            child.SetParent(this);
            return this;
        }

        public Node AddChildren(IEnumerable<Node> children)
        {
            foreach (var child in children)
            {
                AddChild(child);
            }
            return this;
        }

        public Node RemoveChild(Node child)
        {
            _children.Remove(child.Id);
            OnChildRemoved?.Invoke(this, child, false);
            child.SetParent(null);

            return this;
        }

        internal void DestroyChildren()
        {
            foreach ((int id, Node child) in _children)
            {
                _children.Remove(id);
                OnChildRemoved?.Invoke(this, child, true);
                child.Destroy();
            }
        }
    }
}
