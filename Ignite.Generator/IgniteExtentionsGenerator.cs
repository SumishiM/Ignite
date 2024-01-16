using Ignite.Generator.Extentions;
using Ignite.Generator.Metadata;
using Ignite.Generator.Templating;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}

namespace Ignite.Generator
{
    [Generator]
    public sealed class IgniteExtentionGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var potentialComponents = context.PotentialComponents().Collect();

            var compilation = context.CompilationProvider.Combine(potentialComponents);

            context.RegisterSourceOutput(compilation,
                (context, source) => Execute(context, source.Left, source.Right));
        }

        private void Execute(
            SourceProductionContext context,
            Compilation compilation,
            ImmutableArray<TypeDeclarationSyntax> components)
        {
            var code =
                """
                namespace Ignite.Generated;
                
                public static class ClassNames
                {
                    public static string Message = "Hello from Ignite";
                }
                """;

            context.AddSource("ClassNames.g.cs", code);
        }
    }
}
