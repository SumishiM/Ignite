using Microsoft.CodeAnalysis;

namespace Ignite.Generator.Metadata
{
    public sealed class IgniteTypesSymbols
    {
        private const string ComponentInterfaceName = "Ignite.Components.IComponent";
        private const string ComponentLookupTableClassName = "Ignite.Components.ComponentLookupTable";

        public INamedTypeSymbol ComponentTypeSymbol;
        public INamedTypeSymbol ComponentLookupTableTypeSymbol;

        private IgniteTypesSymbols(
            INamedTypeSymbol componentTypeSymbol, 
            INamedTypeSymbol componentLookupTableTypeSymbol) 
        { 
            ComponentTypeSymbol = componentTypeSymbol;
            ComponentLookupTableTypeSymbol = componentLookupTableTypeSymbol;
        }

        public static IgniteTypesSymbols? FromCompilation(Compilation compilation) 
        {
            var componentInterface = compilation.GetTypeByMetadataName(ComponentInterfaceName);
            if (componentInterface is null)
                return null;

            var componentLookupTableClass = compilation.GetTypeByMetadataName(ComponentLookupTableClassName);
            if (componentLookupTableClass is null)
                return null;

            return new(
                componentInterface,
                componentLookupTableClass);
        }
    }
}
