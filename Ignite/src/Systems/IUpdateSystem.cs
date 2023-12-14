using Ignite.Contexts;

namespace Ignite.Systems
{
    public interface IUpdateSystem : ISystem
    {
        void Update(Context context);
    }
}
