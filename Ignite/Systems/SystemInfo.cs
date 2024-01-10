namespace Ignite.Systems
{
    public struct SystemInfo(int contextId, int index, int order)
    {
        public readonly int ContextId { get; init; } = contextId;
        public readonly int Order { get; init; } = order;
        public readonly int Index { get; init; } = index;
        public bool IsActive { get; set; } = false;
    }
}
