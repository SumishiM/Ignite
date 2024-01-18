using Ignite.Generator.Extentions;
using Ignite.Generator.Metadata;
using Ignite.Generator.Templating;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;


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
            ImmutableArray<TypeDeclarationSyntax> potentialComponents)
        {


#if DEBUG
            // Uncomment this if you need to use a debugger.
            //if (!System.Diagnostics.Debugger.IsAttached)
            //{
            //    System.Diagnostics.Debugger.Launch();
            //}
#endif

            var igniteTypesSymbols = IgniteTypesSymbols.FromCompilation(compilation);
            if (igniteTypesSymbols is null)
                return;


            var assemblyTypeFetcher = new AssemblyTypeFetcher(compilation);
            var metadataFetcher = new MetadataFetcher(compilation);

            var parentLookupTableClass = assemblyTypeFetcher
                .GetAllClassesAndSubtypes()
                .Where(t => t.IsSubclassOf(igniteTypesSymbols.ComponentLookupTableTypeSymbol))
                .OrderBy(NumberOfParentClasses)
                .LastOrDefault() ?? igniteTypesSymbols.ComponentLookupTableTypeSymbol;

            var projectName = compilation.AssemblyName?.Replace(".", "") ?? "My";

            var projectMetadata = new TypeMetadata.Project(
                projectName,
                parentLookupTableClass.Name.Replace("ComponentLookupTable", ""),
                parentLookupTableClass.FullName()
            );

            var templates = ImmutableArray.Create(
                FileTemplate.ComponentLookupTableImplementation(projectName),
                FileTemplate.ProjectComponentsImplementation(projectName)
            );

            foreach (var template in templates)
            {
                template.Process(projectMetadata);
            }

            var allTypeMetadata = metadataFetcher.Fetch(igniteTypesSymbols, potentialComponents);

            foreach (var template in templates)
            {
                foreach (var metadata in allTypeMetadata)
                {
                    template.Process(metadata);
                }
            }

            foreach (var template in templates)
            {
                context.AddSource(template.FileName, template.GetDocumentWithReplacements());
            }

        }

        private static int NumberOfParentClasses(INamedTypeSymbol type)
            => type.BaseType is null ? 0 : 1 + NumberOfParentClasses(type.BaseType);
    }
}
