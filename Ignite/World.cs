using Ignite.Components;

namespace Ignite
{
    public class World
    {
        public ComponentLookupTable Lookup { get; set; }

        public Dictionary<int, Node> Nodes { get; set; }

        public World ()
        {
            Node node = new Node(this);
        }
    }
}
