using Ignite.Generator.Extentions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ignite.Generator.Metadata
{
    public sealed class MetadataFetcher
    {
        private readonly Compilation _compilation;

        public MetadataFetcher(Compilation compilation)
        {
            _compilation = compilation;
        }

        public IEnumerable<TypeMetadata> FetchMetadata(
            IgniteTypeSymbols igniteTypeSymbols,
            ImmutableArray<TypeDeclarationSyntax> potentialComponents)
        {
            var allValueTypes = potentialComponents
                .SelectMany(ValueTypeFromTypeDeclarationSyntax)
                .ToImmutableArray();

            var componentIndexOffset = 0;
            var components = FetchComponents(igniteTypeSymbols, allValueTypes);
            foreach ( var component in components )
            {
                yield return component;
                componentIndexOffset++;
            }

        }

        private IEnumerable<TypeMetadata.Component> FetchComponents(
            IgniteTypeSymbols igniteTypeSymbols,
            ImmutableArray<INamedTypeSymbol> allValueTypes)
            => allValueTypes
            .Where(t => !t.IsGenericType && t.ImplementsInterface(igniteTypeSymbols.ComponentInterface))
            .OrderBy(c => c.Name)
            .Select((component, index) => new TypeMetadata.Component(
                Index: index,
                Name: component.Name.ToCleanComponentName(),
                FullName: component.FullTypeName(),
                IsInternal: component.DeclaredAccessibility == Accessibility.Internal,
                Constructors: component.Constructors
                    .Where(c => c.DeclaredAccessibility == Accessibility.Public)
                    .Select(ConstructorMetadataFromConstructor)
                    .ToImmutableArray()));

        private ConstructorMetadata ConstructorMetadataFromConstructor(IMethodSymbol methodSymbol)
            => new (
                methodSymbol.Parameters
                    .Select(p => new ConstructorParameter(p.Name, p.Type.FullTypeName()))
                    .ToImmutableArray());

        private IEnumerable<INamedTypeSymbol> ValueTypeFromTypeDeclarationSyntax(
            TypeDeclarationSyntax typeDeclarationSyntax)
        {
            var semanticModel = _compilation.GetSemanticModel(typeDeclarationSyntax.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(typeDeclarationSyntax) is not INamedTypeSymbol potentialComponentTypeSymbol)
                return Enumerable.Empty<INamedTypeSymbol>();

            if (typeDeclarationSyntax is RecordDeclarationSyntax && !potentialComponentTypeSymbol.IsValueType)
                return Enumerable.Empty<INamedTypeSymbol>();

            return potentialComponentTypeSymbol.Yield();
        }
    }
}
