using System.Collections.Immutable;

namespace Ignite.Generator.Metadata
{
    public sealed record ConstructorParameter(
        string Name,
        string FullTypeName);

    public sealed record ConstructorMetadata(
        ImmutableArray<ConstructorParameter> Parameters);
}
