using System.Collections.Immutable;

namespace Ignite
{
    /** 
     * This part of the Node class is responsible of the hierarchy for nodes
     * 
    **/
    public partial class Node
    {
        protected Node? _parent;
        public Node? Parent => _parent;


        private Dictionary<int,  Node> _children;
        internal Dictionary<int, Node> ChildrenIndex => _children;
        public ImmutableArray<Node> Children => _children.Values.ToImmutableArray();
    }
}
