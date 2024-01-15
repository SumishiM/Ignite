using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ignite.Generator.Extentions
{
    public static class IncrementalGeneratorExtentions
    {
        public static IncrementalValuesProvider<TypeDeclarationSyntax> PotentialComponents(
            this IncrementalGeneratorInitializationContext context)
            => context.SyntaxProvider.CreateSyntaxProvider(
                (node, _) => node.IsStructOrRecordWithSubtypes(),
                (c, _) => (TypeDeclarationSyntax)c.Node);

        // for classes if needed
        /*
            //public static IncrementalValueProvider<ClassDeclarationSyntax> PotentialClass(
                this IncrementalGeneratorInitializationContext context)
                => context.SyntaxProvider.CreateSyntaxProvider(
                    (node, _) => node.IsClassWithSubtypes(),
                    (c, _) => (TypeDeclarationSyntax)c.Node);
            )
        */

        public static bool IsClassWithSubtypes(this SyntaxNode node)
            => node is ClassDeclarationSyntax { BaseList.Types.Count: > 0 }; 

        public static bool IsStructOrRecordWithSubtypes(this SyntaxNode node)
            => node is
                RecordDeclarationSyntax { BaseList.Types.Count: > 0 } or
                StructDeclarationSyntax { BaseList.Types.Count: > 0 };
    }
}
