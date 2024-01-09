namespace Ignite.Systems
{
    public struct SystemInfo(int contextId, int order)
    {
        public int ContextId { get; init; } = contextId;
        public readonly int Order { get; init; } = order;
        public bool IsActive { get; init; } = false;
    }
}
