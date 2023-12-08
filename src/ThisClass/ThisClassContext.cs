using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.All)]
    public sealed class IsExternalInit : Attribute
    {
    }
}

namespace ThisClass
{
    public sealed record ThisClassContext(SyntaxNode SyntaxNode,
                                          TypeDeclarationSyntax TypeDeclaration,
                                          INamedTypeSymbol TypeSymbol,
                                          SemanticModel SemanticModel)
    {
        private static readonly SyntaxTokenList PartialModifier = SyntaxTokenList.Create(SyntaxFactory.Token(SyntaxKind.PartialKeyword));

        public string TypeName => TypeSymbol.Name;

        public SyntaxList<MemberDeclarationSyntax> Members
        {
            get => TypeDeclaration.Members;
            init => TypeDeclaration = TypeDeclaration.WithMembers(value);
        }

        public static ThisClassContext FromTypeSymbol(SyntaxNode syntaxNode, INamedTypeSymbol namedTypeSymbol, SemanticModel semanticModel)
        {
            var node = ((TypeDeclarationSyntax)syntaxNode).WithAttributeLists(default).WithMembers(default).WithoutTrivia().WithModifiers(PartialModifier);
            return new ThisClassContext(syntaxNode, node, namedTypeSymbol, semanticModel);
        }

        public SourceText CreateSourceText()
        {
            // Traverse ancestor syntax to determine outer scope
            MemberDeclarationSyntax content = TypeDeclaration;
            var originalContent = SyntaxNode;
            while (originalContent.Parent is TypeDeclarationSyntax type)
            {
                content = type.WithMembers(SyntaxFactory.SingletonList(content))
                    .WithModifiers(PartialModifier)
                    .WithoutTrivia()
                    .WithAttributeLists(default);
                originalContent = originalContent.Parent!;
            }

            while (originalContent.Parent is BaseNamespaceDeclarationSyntax @namespace)
            {
                content = @namespace.WithMembers(SyntaxFactory.SingletonList(content))
                    .WithoutTrivia()
                    .WithAttributeLists(default);
                originalContent = originalContent.Parent!;
            }

            var contentAnnotation = new SyntaxAnnotation("content");
            var root = SyntaxNode.SyntaxTree.GetCompilationUnitRoot().ReplaceNode(originalContent, content.WithAdditionalAnnotations(contentAnnotation));
            var scopedSemanticModel = SemanticModel.Compilation.AddSyntaxTrees(root.SyntaxTree).GetSemanticModel(root.SyntaxTree);

            var rewriter = new QualifiedNameSyntaxRewriter(scopedSemanticModel);
            var compilationUnit = SyntaxFactory.CompilationUnit(
                externs: default,
                usings: default,
                attributeLists: default,
                members: SyntaxFactory.SingletonList(
                    (MemberDeclarationSyntax)rewriter.Visit(root)
                    .DescendantNodes()
                    .First(x => x.HasAnnotation(contentAnnotation))))
                .WithLeadingTrivia(ThisClassGenerator.LeadingCompilationTrivia);
            return compilationUnit.NormalizeWhitespace().GetText(Encoding.UTF8);
        }
    }
}
