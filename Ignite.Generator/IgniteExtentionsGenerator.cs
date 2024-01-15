using Ignite.Generator.Extentions;
using Ignite.Generator.Metadata;
using Ignite.Generator.Templating;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace Ignite.Generator
{
    internal class IgniteExtentionsGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var potentialComponents = context.PotentialComponents().Collect();
            var compilation = potentialComponents
                .Combine(context.CompilationProvider);

            context.RegisterSourceOutput(compilation, (c, t) => Execute(c, t.Right, t.Left));
        }

        public void Execute(
            SourceProductionContext context,
            Compilation compilation,
            ImmutableArray<TypeDeclarationSyntax> potentialComponents)
        {
            var igniteTypeSymbols = IgniteTypeSymbols.FromCompilation(compilation);
            if (igniteTypeSymbols is null)
                return;

            var referencedAssemblyTypeFetcher = new ReferencedAssemblyTypeFetcher(compilation);
            var parentLookupTableClass = referencedAssemblyTypeFetcher
                .GetAllCompiledClassesWithSubtypes()
                .Where(t => t.IsSubclassOf(igniteTypeSymbols.ComponentLookupClass))
                .OrderBy(NumberOfParentClasses)
                .LastOrDefault() ?? igniteTypeSymbols.ComponentLookupClass;

            var projectName = compilation.AssemblyName?.Replace(".", "") ?? "My";

            var metadataFetcher = new MetadataFetcher(compilation);

            var templates = ImmutableArray.Create(
                Templates.ComponentTypes(projectName),
                Templates.LookupImplementation(projectName)
            );

            var projectMetaData = new TypeMetadata.Project(
                projectName,
                parentLookupTableClass.Name.Replace("ComponentLookupTable", ""),
                parentLookupTableClass.FullTypeName());

            foreach (var template in templates)
            {
                template.Process(projectMetaData);
            }

            var allTypeMetadata =
                metadataFetcher.FetchMetadata(
                    igniteTypeSymbols,
                    potentialComponents);

            foreach (var metadata in allTypeMetadata)
            {
                foreach (var template in templates)
                {
                    template.Process(metadata);
                }
            }

            foreach (FileTemplate template in templates)
            {
                context.AddSource(template.FileName, template.GetDocumentWithreplacements());
            }
        }

        private static int NumberOfParentClasses(INamedTypeSymbol typeSymbol)
            => typeSymbol.BaseType is null ? 0 : 1 + NumberOfParentClasses(typeSymbol.BaseType);
    }
}
