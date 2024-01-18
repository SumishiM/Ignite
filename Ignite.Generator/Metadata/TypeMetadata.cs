using System.Collections.Immutable;

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
            ImmutableArray<ConstructorMetadata> Constructors
            ) : TypeMetadata;
    }
}
