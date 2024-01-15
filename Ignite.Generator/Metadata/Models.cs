using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Security.Cryptography.X509Certificates;

namespace Ignite.Generator.Metadata
{
    public sealed class IgniteTypeSymbols
    {
        public INamedTypeSymbol ComponentInterface { get; }
        public INamedTypeSymbol ComponentLookupClass { get; }

        private IgniteTypeSymbols(
            INamedTypeSymbol componentInterface,
            INamedTypeSymbol componentLookupClass)
        {
            ComponentInterface = componentInterface;
            ComponentLookupClass = componentLookupClass;
        }

        public static IgniteTypeSymbols? FromCompilation(Compilation compilation)
        {
            var componentInterface = compilation.GetTypeByMetadataName("Ignite.Components.IComponent");
            if (componentInterface == null)
                return null;

            var componentLookupClass = compilation.GetTypeByMetadataName("Ignite.Components.ComponentLookupTable");
            if (componentLookupClass == null)
                return null;

            return new(
                componentInterface,
                componentLookupClass
            );
        }
    }

    public sealed record ConstructorParameter(
        string Name,
        string FullTypeName
    );

    public sealed record ConstructorMetadata(
        ImmutableArray<ConstructorParameter> Parameters
    );

    public abstract record TypeMetadata
    {
        public sealed record Project(
                string ProjectName,
                string? ParentProjectName,
                string ParentProjectLookupClassName
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
