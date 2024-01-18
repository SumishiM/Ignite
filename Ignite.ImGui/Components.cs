using Ignite.Attributes;
using Ignite.Components;
using Ignite.Systems;

namespace Ignite.UI
{
    public class Move : IComponent
    {
    }

    public class Jump : IComponent
    {
    }

    [Filter(Context.AccessFilter.AllOf, typeof(Move))]
    public class MoveSystem : IStartSystem
    {
        public void Start(Context context)
        {
            Console.WriteLine(context.Nodes.Length);
        }
        public void Dispose()
        {
            Console.WriteLine(typeof(MoveSystem).Name + ".Dispose()");
        }
    }
}
