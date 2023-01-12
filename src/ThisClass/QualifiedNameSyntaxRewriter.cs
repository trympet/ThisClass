using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ThisClass
{
    public class QualifiedNameSyntaxRewriter : CSharpSyntaxRewriter
    {
        private readonly SemanticModel semanticModel;

        public QualifiedNameSyntaxRewriter(SemanticModel semanticModel)
        {
            this.semanticModel = semanticModel;
        }
        public override SyntaxNode? VisitSimpleBaseType(SimpleBaseTypeSyntax node)
        {
            return node.WithType(EnsureQualifiedTypeSyntax(node.Type));
        }

        public override SyntaxNode? VisitTypeConstraint(TypeConstraintSyntax node)
        {
            return node.WithType(EnsureQualifiedTypeSyntax(node.Type));
        }

        private TypeSyntax EnsureQualifiedTypeSyntax(TypeSyntax type)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(type);
            var name = symbolInfo.Symbol?.ToDisplayString(ThisClassGenerator.FullyQualifiedGlobalDisplayFormat);
            return name is not null ? SyntaxFactory.ParseTypeName(name) : type;
        }
    }
}
