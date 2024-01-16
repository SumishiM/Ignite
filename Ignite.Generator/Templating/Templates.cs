using System;
using System.Collections.Generic;
using System.Text;

namespace Ignite.Generator.Templating
{
    public static partial class Templates
    {
        public const string GeneratedCodeNamespace = "Ignite.Generated";
        public const string ProjectNameToken = "<project_name>";

        public const string ComponentLookupTable =
            $$"""
            namespace {{GeneratedCodeNamespace}};

            public class {{ProjectNameToken}}ComponentLookupTable : global::Ignite.Components.ComponentLookupTable
            {

            }
            """;
    }
}
