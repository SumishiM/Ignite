namespace Ignite.Generator.Templating
{
    public partial class Templates
    {
        public const string GeneratedCodeNamespace = "Ignite.Generated";
        public const string ProjectNameToken = "<project_name>";
        public const string ComponentIndexListToken = "<component_index_list>";
        public const string ComponentTypeToIndexToken = "<component_type_to_index>";
        public const string ParentComponentLookupTable = "<parent_project_lookup>";

        public const string ComponentLookupTableImplementationRaw =
            $$"""
            using System.Collections;
            using System.Collections.Immutable;

            namespace {{GeneratedCodeNamespace}};

            public class {{ProjectNameToken}}ComponentLookupTable : {{ParentComponentLookupTable}}
            {
                public {{ProjectNameToken}}ComponentLookupTable()
                {
                    _componentsIndex = base._componentsIndex
                        .Concat(_{{ProjectNameToken}}ComponentsIndex)
                        .ToImmutableDictionary();
                }

                private readonly ImmutableDictionary<Type, int> _{{ProjectNameToken}}ComponentsIndex = 
                    new Dictionary<Type, int>()
                    {
            {{ComponentTypeToIndexToken}}        }
                    .ToImmutableDictionary();
            }
            """;


        public const string ComponentTypesRaw =
            $$"""
            namespace {{GeneratedCodeNamespace}};

            /// <summary>
            /// Collection of all ids for fetching components declared in this project.
            /// </summary>
            public static class {{ProjectNameToken}}Components
            {
            {{ComponentIndexListToken}}}
            """;
    }
}
