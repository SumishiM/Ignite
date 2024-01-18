using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Ignite.Generator.Metadata
{
    public sealed record ConstructorParameter(
        string Name,
        string FullTypeName);

    public sealed record ConstructorMetadata(
        ImmutableArray<ConstructorParameter> Parameters);
}
