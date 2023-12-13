using Ignite.Contexts;

namespace Ignite.Systems
{
    public interface IStartupSystem : ISystem
    {
        void Start(Context context);
    }
}
