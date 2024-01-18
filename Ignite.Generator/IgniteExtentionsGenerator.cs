using Ignite.Generator.Extentions;
using Ignite.Generator.Metadata;
using Ignite.Generator.Templating;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography.X509Certificates;


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
            var igniteTypesSymbols = IgniteTypesSymbols.FromCompilation(compilation);
            var projectName = compilation.AssemblyName?.Replace(".", "") ?? "My";
            var projectMetadata = new TypeMetadata.Project(
                projectName, null, string.Empty);




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
