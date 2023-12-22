using Ignite.Components;

namespace Ignite
{
    public class World
    {
        public ComponentLookupTable Lookup { get; set; }

        public Node Root { get; private set; }
        public Dictionary<int, Node> Nodes { get; set; }


        public World ()
        {
            Root = Node.CreateBuilder(this).ToNode();
        }
    }
}
