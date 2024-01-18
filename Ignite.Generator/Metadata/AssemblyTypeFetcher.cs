using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Ignite.Generator.Metadata
{
    public sealed class AssemblyTypeFetcher
    {
        private readonly Compilation _compilation;
        private ImmutableArray<INamedTypeSymbol>? _cacheOfAllTypesInAssemblies;

        public AssemblyTypeFetcher(Compilation compilation)
        {
            _compilation = compilation;
        }

        public ImmutableArray<INamedTypeSymbol> GetAllClassesAndSubtypes()
            => GetAllTypesInAssemblies()
                .Where(t => !t.IsValueType && t.BaseType is not null)
                .ToImmutableArray();

        private ImmutableArray<INamedTypeSymbol> GetAllTypesInAssemblies()
        {
            if (_cacheOfAllTypesInAssemblies is not null)
                return _cacheOfAllTypesInAssemblies.Value;

            var allTypesInAssemblies =
                _compilation.SourceModule.ReferencedAssemblySymbols
                    .SelectMany(assemblySymbol =>
                        assemblySymbol.GlobalNamespace
                            .GetNamespaceMembers()
                            .SelectMany(GetAllTypesInNamespace))
                    .ToImmutableArray();

            _cacheOfAllTypesInAssemblies = allTypesInAssemblies;
            return allTypesInAssemblies;
        }

        private IEnumerable<INamedTypeSymbol> GetAllTypesInNamespace(INamespaceSymbol namespaceSymbol)
        {
            foreach (var type in namespaceSymbol.GetTypeMembers())
            {
                yield return type;
            }

            var nestedTypes =
                from nestedNamespace in namespaceSymbol.GetNamespaceMembers()
                from nestedType in GetAllTypesInNamespace(nestedNamespace)
                select nestedType;

            foreach (var nestedType in nestedTypes)
            {
                yield return nestedType;
            }
        }

    }
}
