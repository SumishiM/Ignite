using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Ignite.Generator.Metadata
{
    public abstract record TypeMetadata
    {
        public sealed record Project(
            string ProjectName,
            string? ParentProjectName,
            string ParentProjectComponentLookupTableClass
            ) : TypeMetadata;

        public sealed record Component(
            int Index,
            bool IsInternal,
            string Name, 
            string FullName,
            ImmutableArray<Constructor.Metadata> Constructors
            ) : TypeMetadata;
    }
}
