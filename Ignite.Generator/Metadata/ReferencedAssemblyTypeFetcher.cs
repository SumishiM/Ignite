using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ignite.Generator.Metadata
{
    public sealed class ReferencedAssemblyTypeFetcher
    {
        private readonly Compilation _compilation;
        private ImmutableArray<INamedTypeSymbol>? _cacheOfAllTypesInReferencedAssemblies;

        public ReferencedAssemblyTypeFetcher(Compilation compilation)
        {
            _compilation = compilation;
        }

        public ImmutableArray<INamedTypeSymbol> GetAllCompiledClassesWithSubtypes()
            => AllTypesInReferencedAssemblies()
                .Where(typeSymbol => !typeSymbol.IsValueType && typeSymbol.BaseType is not null)
                .ToImmutableArray();

        private ImmutableArray<INamedTypeSymbol> AllTypesInReferencedAssemblies()
        {
            if (_cacheOfAllTypesInReferencedAssemblies is not null)
                return _cacheOfAllTypesInReferencedAssemblies.Value;

            var allTypesInReferencedAssembly =
                _compilation.SourceModule.ReferencedAssemblySymbols
                    .SelectMany(assemnlySymbol =>
                        assemnlySymbol
                            .GlobalNamespace.GetNamespaceMembers()
                            .SelectMany(GetAllTypesInNamespace))
                            .ToImmutableArray();

            _cacheOfAllTypesInReferencedAssemblies = allTypesInReferencedAssembly;
            return allTypesInReferencedAssembly;
        }

        private IEnumerable<INamedTypeSymbol> GetAllTypesInNamespace(
            INamespaceSymbol namespaceSymbol)
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
