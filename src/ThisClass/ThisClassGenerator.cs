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
    [Generator]
    public class ThisClassGenerator : ISourceGenerator
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

        public void Initialize(GeneratorInitializationContext context)
        {
            //System.Diagnostics.Debugger.Launch();
            context.RegisterForSyntaxNotifications(() => new ThisClassSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            AddThisClassAttribute(context);
            if (context.SyntaxReceiver is ThisClassSyntaxReceiver receiver)
            {
                var options = (context.Compilation as CSharpCompilation)?.SyntaxTrees[0].Options as CSharpParseOptions;
                var compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(ThisClassAttributeSourceText, options));

                var thisClassAttributeSymbol = compilation.GetTypeByMetadataName($"ThisClassAttribute");
                if (thisClassAttributeSymbol is null)
                {
                    return;
                }

                foreach (var candidateClass in receiver.CandidateClasses)
                {
                    var semanticModel = compilation.GetSemanticModel(candidateClass.SyntaxTree);
                    var namedTypeSymbol = semanticModel.GetDeclaredSymbol(candidateClass);
                    if (namedTypeSymbol is null)
                    {
                        continue;
                    }

                    if (HasAttribute(namedTypeSymbol, thisClassAttributeSymbol))
                    {
                        AddThisClassToClass(context, namedTypeSymbol);
                    }
                }
            }
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
                    ClassFull = $"{namespaceName}.{className}",
                });

                return thisClassContent;
            }

            return null;
        }

        class ThisClassSyntaxReceiver : ISyntaxReceiver
        {
            public List<ClassDeclarationSyntax> CandidateClasses { get; } = new();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is ClassDeclarationSyntax classDeclarationSyntax)
                {
                    CandidateClasses.Add(classDeclarationSyntax);
                }
            }
        }
    }
}
