using Ignite.Generator.Metadata;

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
            public ComponentTypeToIndexSubstitution () : base(Templates.ComponentTypeToIndexToken) { }

            protected override string? ProcessComponent ( TypeMetadata.Component component )
                => $$"""
                            { typeof(global::{{component.FullName}}), global::{{Templates.GeneratedCodeNamespace}}.{{_projectName}}Components.{{component.Name}} },
                
                """;
        }

        internal sealed class ComponentIndexSubstitution : TemplateSubstitution
        {
            public ComponentIndexSubstitution() : base(Templates.ComponentIndexListToken) { }

            protected override string? ProcessComponent(TypeMetadata.Component component)
            {
                string id = $"global::Ignite.{(string.IsNullOrEmpty(_parentProjectName) ? "Components" : "Generated")}.{_parentProjectName}ComponentLookupTable.{_parentProjectName}NextLookupId + {component.Index}";
                return $$"""
                    public static int {{component.Name}} = {{id}};

                """;
            }
        }

        internal sealed class ParentProjectLookupTableSubstitution : TemplateSubstitution
        {
            public ParentProjectLookupTableSubstitution() : base(Templates.ParentComponentLookupTable) { }

            protected override string? ProcessProject(TypeMetadata.Project project)
            => $"global::{project.ParentProjectComponentLookupTable}";
        }

        internal sealed class IdCountSubstitution : TemplateSubstitution
        {
            private int _idCount;
            public IdCountSubstitution () : base(Templates.NextLookupIdToken) { }

            protected override string? ProcessComponent ( TypeMetadata.Component metadata )
            {
                _idCount++;
                return base.ProcessComponent(metadata);
            }

            protected override string FinalModification ()
                => $"""
                public static int {_projectName}NextLookupId => global::Ignite.{(string.IsNullOrEmpty(_parentProjectName) ? "Components" : "Generated")}.{_parentProjectName}ComponentLookupTable.{_parentProjectName}NextLookupId + {_idCount};

                """;
        }
    }
}
