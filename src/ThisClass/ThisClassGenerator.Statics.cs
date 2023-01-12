using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Scriban;
using System.Linq;
using System.Text;
using ThisClass.Common;

namespace ThisClass
{
    public partial class ThisClassGenerator
    {
        private static readonly SourceText ThisClassAttributeSourceText = SourceText.From(EmbeddedResource.GetContent("ThisClassAttribute.txt"), Encoding.UTF8);
        private static readonly Template ThisClassTemplate = Template.Parse(EmbeddedResource.GetContent("ThisClass.sbntxt"));
        internal static readonly SymbolDisplayFormat FullyQualifiedDisplayFormat = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
        internal static readonly SymbolDisplayFormat FullyQualifiedGlobalDisplayFormat = SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Included);

        public static (string NamespaceName, string ClassDeclaration) GetClassDeclaration(INamedTypeSymbol namedTypeSymbol, Compilation compilation)
        {
            var namespaceName = namedTypeSymbol.ContainingNamespace.ToDisplayString();
            var originalDeclaration = (ClassDeclarationSyntax)namedTypeSymbol.DeclaringSyntaxReferences[0].GetSyntax();
            var rewriter = new QualifiedNameSyntaxRewriter(compilation.GetSemanticModel(originalDeclaration.SyntaxTree));
            var qualifiedDeclaration = (ClassDeclarationSyntax)rewriter.Visit(originalDeclaration);
            var partialClassSyntax = SyntaxFactory.ClassDeclaration(attributeLists: default,
                                                           modifiers: SyntaxTokenList.Create(SyntaxFactory.Token(SyntaxKind.PartialKeyword)),
                                                           identifier: qualifiedDeclaration.Identifier,
                                                           typeParameterList: qualifiedDeclaration.TypeParameterList,
                                                           baseList: qualifiedDeclaration.BaseList,
                                                           constraintClauses: qualifiedDeclaration.ConstraintClauses,
                                                           members: default);
            var className = partialClassSyntax.NormalizeWhitespace().ToFullString().Replace("\r\n{\r\n}", string.Empty);

            return (namespaceName, className);
        }

        public static void AddThisClassToClass(GeneratorExecutionContext context, INamedTypeSymbol namedTypeSymbol)
        {
            var thisClassSource = ProcessClass(namedTypeSymbol, context.Compilation);
            if (thisClassSource is not null)
            {
                context.AddSource($"{namedTypeSymbol.Name}_ThisClass.g", SourceText.From(thisClassSource, Encoding.UTF8));
            }
        }

        public static bool HasAttribute(INamedTypeSymbol symbol, INamedTypeSymbol? attributeSymbol) => symbol
            .GetAttributes()
            .Any(ad => ad?.AttributeClass?.Equals(attributeSymbol, SymbolEqualityComparer.Default) ?? false);

        public static bool HasThisClassAttribute(INamedTypeSymbol symbol, Compilation compilation)
            => HasAttribute(symbol, compilation.GetTypeByMetadataName($"ThisClassAttribute"));

        public static void AddThisClassAttribute(GeneratorExecutionContext context)
        {
            context.AddSource("ThisClassAttribute.g", ThisClassAttributeSourceText);
        }

        private static string? ProcessClass(INamedTypeSymbol namedTypeSymbol, Compilation compilation)
        {
            if (namedTypeSymbol.IsInContainingNamespace())
            {
                var (namespaceName, className) = GetClassDeclaration(namedTypeSymbol, compilation);
                var thisClassContent = ThisClassTemplate.Render(new
                {
                    Namespace = namespaceName,
                    Class = className,
                    ClassFull = namedTypeSymbol.ToDisplayString(FullyQualifiedDisplayFormat),
                });

                return thisClassContent;
            }

            return null;
        }
    }
}
