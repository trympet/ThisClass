using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Threading;

namespace ThisClass
{
    public partial class ThisClassGenerator
    {
        internal static readonly SymbolDisplayFormat FullyQualifiedDisplayFormat = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
        internal static readonly SymbolDisplayFormat FullyQualifiedGlobalDisplayFormat = SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Included);

        public static ThisClassContext AddThisClass(ThisClassContext context)
        {
            var fullName = context.TypeSymbol.ToDisplayString(FullyQualifiedDisplayFormat);
            var fieldDeclaration = SyntaxFactory.ParseMemberDeclaration($"public const string FullName = \"{fullName}\";")!;
            return context with
            {
                Members = context.Members.Add(ThisClassPartialClass.AddMembers(fieldDeclaration!.WithLeadingTrivia(FullNameFieldTrivia)))
            };
        }

        public static bool IsClassDeclaration(SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            return syntaxNode.IsKind(SyntaxKind.ClassDeclaration);
        }
    }
}
