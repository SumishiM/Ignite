using System.Collections.Immutable;

namespace Ignite
{
    public partial class Node
    {
        protected Node? _parent;
        public Node? Parent => _parent;


        private Dictionary<int,  Node> _children;
        internal Dictionary<int, Node> ChildrenIndex => _children;
        public ImmutableArray<Node> Children => _children.Values.ToImmutableArray();
    }
}
