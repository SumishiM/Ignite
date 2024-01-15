using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ignite.Generator.Extentions
{
    public static class HelperExtentions
    {
        private const string Component = "Component";

        private static readonly SymbolDisplayFormat _fullNameDisplayFormat =
            new(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

        public static IEnumerable<T> Yield<T>(this T value)
        {
            yield return value;
        }

        public static bool ImplementsInterface(
            this ITypeSymbol type,
            ISymbol? interfaceToCheck)
            => type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, interfaceToCheck));
    
        public static bool IsSubclassOf(
            this ITypeSymbol type, 
            ISymbol subtypeToCheck)
        {
            ITypeSymbol? nextTypeToCheck = type;

            do
            {
                var subtype = nextTypeToCheck?.BaseType;
                if (subtype is not null && SymbolEqualityComparer.Default.Equals(subtype, subtypeToCheck))
                    return true;
                nextTypeToCheck = subtype;
            }
            while (nextTypeToCheck is not null);

            return false;
        }

        public static string ToCleanComponentName(this string value)
            => value.EndsWith(Component) ? value[..^Component.Length] : value;
    
        public static string FullTypeName(this ITypeSymbol type)
        {
            var fullTypeName = type.ToDisplayString(_fullNameDisplayFormat);
        
            if (fullTypeName.Contains("?") || type is not INamedTypeSymbol { IsGenericType: true } namedTypeSymbol)
            return fullTypeName;

            var generticTypes = string.Join(
                ", ",
                namedTypeSymbol.TypeArguments
                    .Select(x => $"global::{x.FullTypeName()}"));

            return $"{fullTypeName}<{generticTypes}>";
        }
    }

}
