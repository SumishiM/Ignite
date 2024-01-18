using Ignite.Generator.Metadata;
using System.Collections.Immutable;

using static Ignite.Generator.Templating.TemplateSubstitution;

namespace Ignite.Generator.Templating
{
    public class FileTemplate
    {
        public string FileName;
        private readonly string _templateText;
        private readonly ImmutableArray<TemplateSubstitution> _substitutions;

        public FileTemplate(
            string fileName,
            string templateText,
            ImmutableArray<TemplateSubstitution> substitutions)
        {
            FileName = fileName;
            _templateText = templateText;
            _substitutions = substitutions;
        }

        public static FileTemplate ComponentLookupTableImplementation(string projectName)
            => new($"{projectName}ComponentLookupTable.g.cs",
                Templates.ComponentLookupTableImplementationRaw,
                ImmutableArray.Create<TemplateSubstitution>(
                    new ProjectNameSubstitution(),
                    new ParentProjectLookupTableSubstitution(),
                    new ComponentTypeToIndexSubstitution()));

        public static FileTemplate ProjectComponentsImplementation(string projectName)
            => new($"{projectName}Components.g.cs",
                Templates.ComponentTypesRaw,
                ImmutableArray.Create<TemplateSubstitution>(
                    new ProjectNameSubstitution(),
                    new ComponentIndexSubstitution()));
    
        public void Process(TypeMetadata metadata)
        {
            foreach (var substitutions in _substitutions)
            {
                substitutions.Process(metadata);
            }
        }

        public string GetDocumentWithReplacements()
            => _substitutions.Aggregate(
                _templateText,
                (text, substitution) => text.Replace(
                    substitution.TemplateToReplace,
                    substitution.GetProcessedText()));
    }
}
