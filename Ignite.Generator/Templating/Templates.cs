using Ignite.Generator.Metadata;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Ignite.Generator.Templating
{
    public static partial class Templates
    {
        public const string ComponentTypesRawTypes =
            """
            namespace Ignite.Generated
            {
                /// <summary>
                /// Collection of all ids for fetching components declared in this project.
                /// </summary>
                public static class <project_prefix>ComponentTypes
                {<component_id_list>    }
            }
            """;

        /// <summary>
        /// Todo : Change variables !!!!
        /// </summary>
        public const string LookupTableImplementationRaw =
            """
            using System.Collections.Immutable;
            using System.Linq;

            namespace Ignite
            {
                /// <summary>
                /// Auto-generated implementation of <see cref="Ignite.Components.ComponentsLookupTable" /> for this project.
                /// </summary>
                public class <project_prefix>ComponentsLookupTable : <parent_project_lookup>
                {
                    /// <summary>
                    /// First lookup id a <see cref="Ignite.Components.ComponentsLookupTable"/> implementation that inherits from this class must use.
                    /// </summary>
                    <component_count_const>
                    /// <summary>
                    /// Default constructor. This is only relevant for the internals of Bang, so you can ignore it.
                    /// </summary>
                    public <project_prefix>ComponentsLookupTable()
                    {
                        MessagesIndex = base.MessagesIndex.Concat(_messagesIndex).ToImmutableDictionary();
                        ComponentsIndex = base.ComponentsIndex.Concat(_componentsIndex).ToImmutableDictionary();
                        RelativeComponents = base.RelativeComponents.Concat(_relativeComponents).ToImmutableHashSet();
                    }
        
                    private readonly ImmutableHashSet<int> _relativeComponents = new HashSet<int>()
                    {
            <relative_components_set>        }.ToImmutableHashSet();
        
                    private readonly ImmutableDictionary<Type, int> _componentsIndex = new Dictionary<Type, int>()
                    {
            <components_type_to_index>        }.ToImmutableDictionary();
        
                    private readonly ImmutableDictionary<Type, int> _messagesIndex = new Dictionary<Type, int>()
                    {
            <messages_type_to_index>        }.ToImmutableDictionary();
                }
            }
            """;

        public static FileTemplate ComponentTypes(string projectPrefix)
            => new(
                $"{projectPrefix}ComponentTypes.g.cs",
                ComponentTypesRawTypes,
                ImmutableArray.Create<TemplateSubstitution>(
                    new ProjectPrefixSubstitution(),
                    new ComponentIdSubstitution()));

        public static FileTemplate LookupImplementation(string projectPrefix)
            => new(
                $"{projectPrefix}ComponentLookup.g.cs",
                LookupTableImplementationRaw,
                ImmutableArray.Create<TemplateSubstitution>(
                    new ParentProjectLookupClassSubstitution(),
                    new ProjectPrefixSubstitution(),
                    new ComponentTypeToIndexMapSubstitution(),
                    new IdCountSubstitution()));

        public sealed class ParentProjectLookupClassSubstitution : TemplateSubstitution
        {
            public ParentProjectLookupClassSubstitution() : base("<parent_project_lookup>") { }

            protected override string? ProcessProject(TypeMetadata.Project project)
                => $"global::{project.ParentProjectLookupClassName}";
        }

        public sealed class ComponentTypeToIndexMapSubstitution : TemplateSubstitution
        {
            public ComponentTypeToIndexMapSubstitution() : base("<components_type_to_index>") { }

            protected override string? ProcessComponent(TypeMetadata.Component component)
            => $$"""
                            {typeof(global::{{component.FullName}}), global::Ignite.{{ProjectPrefix}}ComponentTypes.{{component.Name}} },

                 """;
        }

        public sealed class IdCountSubstitution : TemplateSubstitution
        {
            private int _idCount;
            public IdCountSubstitution() : base("<component_count_const>") { }

            protected override string? ProcessComponent(TypeMetadata.Component component)
            {
                _idCount++;
                return base.ProcessComponent(component);
            }

            protected override string? FinalModification()
                =>
                $"""
                 public static int {ProjectPrefix}NextLookupTableId => {_idCount} + {ParentProjectPrefix}ComponentsLookupTable.{ParentProjectPrefix}Id;
                 """;
        }

        private sealed class ComponentIdSubstitution : TemplateSubstitution
        {
            public ComponentIdSubstitution() : base("<component_id_list>") { }

            protected override string? ProcessComponent(TypeMetadata.Component component)
            {
                var id = $"global::Ignite.{ParentProjectPrefix}ComponentLookupTable.{ParentProjectPrefix}NextLookupId + {component.Index}";

                return
                    $"""
                            /// <summary>
                            /// Unique Id used for the lookup of components with type <see cref="{component.FullName}"/>.
                            /// </summary>
                            public static int {component.Name} => {id};
                     """;
            }
        }
    }
}
