using Ignite.Attributes;
using Ignite.Components;
using Ignite.Systems;
using System.Numerics;

namespace Ignite.UI
{
    public class Move : IComponent
    {
        public Vector2 Position;
    }

    public class Jump : IComponent
    {
    }

    [FilterComponent(typeof(Move))]
    public class SetStartMoveSystem : IStartSystem
    {
        public void Start(Context context)
        {
            foreach (Node node in context.Nodes)
            {
                node.GetComponent<Move>().Position = Vector2.One;
                Console.WriteLine(node.Name + " -> " + node.GetComponent<Move>().Position);
            }
        }

        public void Dispose()
        {
            Console.WriteLine(nameof(SetStartMoveSystem.Dispose));
        }
    }

    [FilterComponent(typeof(Move))]
    public class UpdateMoveSystem : IUpdateSystem
    {
        public void Update(Context context)
        {
            foreach (Node node in context.Nodes)
            {
                node.GetComponent<Move>().Position += Vector2.One;
                Console.WriteLine(node.Name + "->" + node.GetComponent<Move>().Position);
            }
        }

        public void Dispose()
        {
            Console.WriteLine(nameof(UpdateMoveSystem.Dispose));
        }
    }

    [FilterComponent(typeof(Move))]
    public class SetExitMoveSystem : IExitSystem
    {
        public void Exit(Context context)
        {
            foreach (Node node in context.Nodes)
            {
                node.GetComponent<Move>().Position = Vector2.Zero;
                Console.WriteLine(node.Name + "->" + node.GetComponent<Move>().Position);
            }
        }

        public void Dispose()
        {
            Console.WriteLine(nameof(SetExitMoveSystem.Dispose));
        }
    }
}
