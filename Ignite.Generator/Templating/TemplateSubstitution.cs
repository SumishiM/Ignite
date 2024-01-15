using Ignite.Generator.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ignite.Generator.Templating
{
    public abstract class TemplateSubstitution
    {
        private readonly StringBuilder _aggregatedText = new();

        protected string ProjectPrefix = "";
        protected string ParentProjectPrefix = "";

        public string StringToReplaceInTemplate { get; }

        protected TemplateSubstitution(string stringToReplaceInTemplate)
        {
            StringToReplaceInTemplate = stringToReplaceInTemplate;
        }

        public void Process(TypeMetadata metadata)
        {
            var result = metadata switch
            {
                TypeMetadata.Project project => SaveAndProcessProject(project),
                TypeMetadata.Component component => ProcessComponent(component),
                _ => throw new InvalidOperationException()
            };

            if (result is not null)
            {
                _aggregatedText.Append(result);
            }
        }

        private string? SaveAndProcessProject(TypeMetadata.Project project)
        {
            ProjectPrefix = project.ProjectName;
            ParentProjectPrefix = project.ParentProjectName ?? "";
            return ProcessProject(project);
        }

        protected virtual string? ProcessProject(TypeMetadata.Project project) => null;
        protected virtual string? ProcessComponent(TypeMetadata.Component component) => null;
        protected virtual string? FinalModification() => null;

        public string GetProcessedText()
        {
            var finalModification = FinalModification();
            if (finalModification is not null)
                _aggregatedText.Append(finalModification);
            return _aggregatedText.ToString();
        }
    }
}
