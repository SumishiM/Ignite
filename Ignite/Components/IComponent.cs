namespace Ignite.Components
{
    public interface IComponent
    {
        public Node Parent { get; internal set; }
    }
}
