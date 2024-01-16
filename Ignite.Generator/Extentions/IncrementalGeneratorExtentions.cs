using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Ignite.Generator.Extentions
{
    public static class IncrementalGeneratorExtentions
    {

        /// <summary>
        /// Get every syntax that can potentially be a component
        /// </summary>
        public static IncrementalValuesProvider<TypeDeclarationSyntax> PotentialComponents(
            this IncrementalGeneratorInitializationContext context)
            => context.SyntaxProvider.CreateSyntaxProvider(
                predicate: (c, _) => c is TypeDeclarationSyntax,
                transform: (node, _) => (TypeDeclarationSyntax)node.Node)
                    .Where(c => c is not null);
    }
}
