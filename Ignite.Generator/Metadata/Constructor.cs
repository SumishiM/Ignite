using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Ignite.Generator.Metadata
{
    public sealed class Constructor
    {
        public sealed record Parameter(
            string Name, 
            string FullTypeName);

        public sealed record Metadata(
            ImmutableArray<Parameter> Parameters);
    }
}
