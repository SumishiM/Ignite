using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Ignite
{
    /** 
     * This part of the Node class is handle the nodes hierarchy
    **/
    public partial class Node
    {
        public event Action<Node, Node?> OnParentChanged;
        public event Action<Node, Node> OnChildAdded;
        public event Action<Node, Node> OnChildRemoved;

        protected Node? _parent;
        public Node? Parent => _parent;


        private readonly Dictionary<int,  Node> _children = new();
        internal Dictionary<int, Node> ChildrenIndex => _children;
        public ImmutableArray<Node> Children => _children.Values.ToImmutableArray();

        public void SetParent(Node? parent)
        {
            _parent = parent;
            OnParentChanged.Invoke(this, parent);
        }

        public void AddChild(Node child)
        {
            _children[child.Id] = child;
            OnChildAdded.Invoke(this, child);
            child.SetParent(this);
        }

        public void AddChildren(IEnumerable<Node> children)
        {
            foreach (var child in children)
            {
                AddChild(child);
            }
        }

        public void RemoveChild(Node child) 
        { 
            _children.Remove(child.Id);
            OnChildRemoved.Invoke(this, child);
            child.SetParent(null);
        }
    }
}
