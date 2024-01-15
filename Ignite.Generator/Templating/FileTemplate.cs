using Ignite.Generator.Metadata;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ignite.Generator.Templating
{
    public sealed class FileTemplate
    {
        private readonly string _templateText;
        private readonly ImmutableArray<TemplateSubstitution> _substitutions;

        public string FileName { get; }

        public FileTemplate(
            string fileName,
            string templateText,
            ImmutableArray<TemplateSubstitution> substitutions)
        {
            FileName = fileName;
            _templateText = templateText;
            _substitutions = substitutions;
        }

        public void Process(TypeMetadata metadata)
        {
            foreach (var substitution in _substitutions)
            {
                substitution.Process(metadata);
            }
        }

        public string GetDocumentWithreplacements() => _substitutions.Aggregate(
            _templateText,
            (text, substitution) => text.Replace(
                substitution.StringToReplaceInTemplate,
                substitution.GetProcessedText()));
    }

    internal sealed class ProjectPrefixSubstitution : TemplateSubstitution
    {
        public ProjectPrefixSubstitution() : base("<project_prefix>") { }

        protected override string? ProcessProject(TypeMetadata.Project project)
            => project.ProjectName;
    }
}
