using Ignite.Generator.Extentions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace Ignite.Generator.Metadata
{
    public sealed class MetadataFetcher
    {
        private readonly Compilation _compilation;
        
        public MetadataFetcher(Compilation compilation)
        {
            _compilation = compilation;
        }

        /// <summary>
        /// Fetch every data we want for the generated code. Such as components, and create the metadata related to it.
        /// </summary>
        public IEnumerable<TypeMetadata> Fetch(
            IgniteTypesSymbols igniteTypesSymbols, 
            ImmutableArray<TypeDeclarationSyntax> potentialComponents)
        {
            var allValueType = potentialComponents
                .SelectMany(ValueTypeFromTypeDeclarationSyntax)
                .ToImmutableArray();

            var components = FetchComponents(igniteTypesSymbols, allValueType);
            foreach ( var component in components )
            {
                yield return component;
            }
        }

        /// <summary>
        /// Fetch components data and create the metadata for it
        /// </summary>
        private IEnumerable<TypeMetadata.Component> FetchComponents(
            IgniteTypesSymbols igniteTypesSymbols, 
            ImmutableArray<INamedTypeSymbol> allValueTypes)
            => allValueTypes
                .Where(t => 
                    (!t.IsGenericType || !t.IsAbstract) 
                    && t.ImplementInterface(igniteTypesSymbols.ComponentTypeSymbol)
                    && !string.IsNullOrEmpty(t.Name.ToCleanComponentName()))
                .OrderBy(c => c.Name)
                .Select((component, index) => new TypeMetadata.Component(
                    Index: index,
                    Name: component.Name.ToCleanComponentName(),
                    FullName: component.FullName(),
                    IsInternal: component.DeclaredAccessibility == Accessibility.Internal,
                    Constructors: component.Constructors
                        .Where(c => c.DeclaredAccessibility == Accessibility.Public) 
                        .Select(ConstructorMetadataFromSymbol)
                        .ToImmutableArray()));

        private ConstructorMetadata ConstructorMetadataFromSymbol(IMethodSymbol symbol)
            => new(
                symbol.Parameters
                    .Select(p => new ConstructorParameter(p.Name, p.Type.FullName()))
                    .ToImmutableArray());    

        /// <summary>
        /// Get every value types for syntax we are interessed in
        /// </summary>
        private IEnumerable<INamedTypeSymbol> ValueTypeFromTypeDeclarationSyntax(
            TypeDeclarationSyntax typeDeclarationSyntax)
        {
            var sementic = _compilation.GetSemanticModel(typeDeclarationSyntax.SyntaxTree);
            if( sementic.GetDeclaredSymbol(typeDeclarationSyntax) is not INamedTypeSymbol potentialComponentSymbol)
                return Enumerable.Empty<INamedTypeSymbol>();

            if (typeDeclarationSyntax is RecordDeclarationSyntax && !potentialComponentSymbol.IsValueType)
                return Enumerable.Empty<INamedTypeSymbol>();

            return potentialComponentSymbol.Yield();
        }
    }
}
