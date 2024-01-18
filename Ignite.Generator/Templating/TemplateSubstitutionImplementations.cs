using Ignite.Generator.Metadata;
using Microsoft.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;

namespace Ignite.Generator.Templating
{
    public abstract partial class TemplateSubstitution
    {
        internal sealed class ProjectNameSubstitution : TemplateSubstitution
        {
            public ProjectNameSubstitution() : base(Templates.ProjectNameToken) { }

            protected override string? ProcessProject(TypeMetadata.Project project)
                => project.ProjectName;
        }

        internal sealed class ComponentTypeToIndexSubstitution : TemplateSubstitution
        {
            public ComponentTypeToIndexSubstitution() : base(Templates.ComponentTypeToIndexToken) { }

            protected override string? ProcessComponent(TypeMetadata.Component component)
                => $$"""
                            { typeof(global::{{component.FullName}}), {{Templates.GeneratedCodeNamespace}}.{{_projectName}}ComponentTypes.{{component.Name}} },
                """;
        }
        internal sealed class ComponentIndexSubstitution : TemplateSubstitution
        {
            public ComponentIndexSubstitution() : base(Templates.ComponentIndexListToken) { }

            protected override string? ProcessComponent(TypeMetadata.Component component)
            {
                string id = $"global::Ignite.Components.{_parentProjectName}ComponentLookupTable.{_parentProjectName}NextLookupId + {component.Index}";
                return $$"""
                    public static int {{component.Name}} = {{id}};
                """;
            }


        }
    }
}
