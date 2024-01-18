using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ignite.Generator.Extentions
{
    public static class HelperExtentions
    {
        public static bool ImplementInterface(this INamedTypeSymbol symbol, INamedTypeSymbol interfaceToCheck)
            => symbol.AllInterfaces
                .Any(interfaceSymbol => SymbolEqualityComparer.Default.Equals(interfaceSymbol, interfaceToCheck));

        public static IEnumerable<T> Yield<T>(this T obj)
        {
            yield return obj;
        }


        private static readonly SymbolDisplayFormat _fullNameDisplayFormat =
            new(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

        public static string FullName(this ITypeSymbol symbol)
        {
            var fullTypeName = symbol.ToDisplayString(_fullNameDisplayFormat);

            if (fullTypeName.Contains("?") || symbol is not INamedTypeSymbol { IsGenericType: true } namedTypeSymbol)
                return fullTypeName;

            var genericTypes = string.Join(", ",
                namedTypeSymbol.TypeArguments.Select(x => $"global::{x.FullName()}"));
            return $"{fullTypeName}<{genericTypes}>";
        }

        private const string Component = "Component";

        public static string ToCleanComponentName(this string value)
            => value.EndsWith(Component) ? value[..^Component.Length] : value;
    }
}
