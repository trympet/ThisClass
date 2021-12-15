using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Scriban;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThisClass.Common;

namespace ThisClass
{
    public partial class ThisClassGenerator
    {
        private static readonly SourceText ThisClassAttributeSourceText = SourceText.From(EmbeddedResource.GetContent("ThisClassAttribute.txt"), Encoding.UTF8);
        private static readonly Template ThisClassTemplate = Template.Parse(EmbeddedResource.GetContent("ThisClass.sbntxt"));

        public static void AddThisClassToClass(GeneratorExecutionContext context, INamedTypeSymbol namedTypeSymbol)
        {
            var thisClassSource = ProcessClass(namedTypeSymbol);
            if (thisClassSource is not null)
            {
                context.AddSource($"{namedTypeSymbol.Name}_ThisClass.cs", SourceText.From(thisClassSource, Encoding.UTF8));
            }
        }

        public static bool HasAttribute(INamedTypeSymbol symbol, INamedTypeSymbol? attributeSymbol) => symbol
            .GetAttributes()
            .Any(ad => ad?.AttributeClass?.Equals(attributeSymbol, SymbolEqualityComparer.Default) ?? false);

        public static bool HasThisClassAttribute(INamedTypeSymbol symbol, Compilation compilation)
            => HasAttribute(symbol, compilation.GetTypeByMetadataName($"ThisClassAttribute"));

        public static void AddThisClassAttribute(GeneratorExecutionContext context)
        {
            context.AddSource("ThisClassAttribute.cs", ThisClassAttributeSourceText);
        }

        private static string? ProcessClass(INamedTypeSymbol namedTypeSymbol)
        {
            if (namedTypeSymbol.IsInContainingNamespace())
            {
                var namespaceName = namedTypeSymbol.ContainingNamespace.ToDisplayString();
                var classNameFormat = new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeTypeConstraints | SymbolDisplayGenericsOptions.IncludeVariance);
                var className = namedTypeSymbol.ToDisplayString(classNameFormat);

                var thisClassContent = ThisClassTemplate.Render(new
                {
                    Namespace = namespaceName,
                    Class = className,
                    ClassFull = namedTypeSymbol.ToDisplayString(new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces)),
                });

                return thisClassContent;
            }

            return null;
        }
    }
}
