using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Scriban;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThisClass;
using ThisClass.Common;

namespace NLog.Extensions.ThisClass
{
    [Generator]
    internal class IThisClassGenerator : ISourceGenerator
    {
        private static readonly SourceText IThisClassSourceText = SourceText.From(EmbeddedResource.GetContent("ThisClassAttribute.IThisClass.txt"), Encoding.UTF8);
        private static readonly SourceText ThisClassExtensionsSourceText = SourceText.From(EmbeddedResource.GetContent("ThisClassExtensions.txt"), Encoding.UTF8);
        private static readonly SourceText ClassLoggerAttributeSourceText = SourceText.From(EmbeddedResource.GetContent("ClassLoggerAttribute.txt"), Encoding.UTF8);
        private static readonly Template IThisClassImplTemplate = Template.Parse(EmbeddedResource.GetContent("IThisClassImpl.sbntxt"));
        private static readonly Template ClassLoggerTemplate = Template.Parse(EmbeddedResource.GetContent("ClassLogger.sbntxt"));

        public void Initialize(GeneratorInitializationContext context)
        {
            //System.Diagnostics.Debugger.Launch();
            context.RegisterForSyntaxNotifications(() => new IThisClassSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            context.AddSource("ThisClassAttribute.IThisClass.cs", IThisClassSourceText);
            context.AddSource("ThisClassExtensions.cs", ThisClassExtensionsSourceText);
            context.AddSource("ClassLoggerAttribute.cs", ClassLoggerAttributeSourceText);

            if (context.SyntaxReceiver is IThisClassSyntaxReceiver receiver)
            {
                var options = (context.Compilation as CSharpCompilation)?.SyntaxTrees[0].Options as CSharpParseOptions;
                var compilation = context.Compilation.AddSyntaxTrees(
                    CSharpSyntaxTree.ParseText(IThisClassSourceText, options),
                    CSharpSyntaxTree.ParseText(ThisClassExtensionsSourceText, options),
                    CSharpSyntaxTree.ParseText(ClassLoggerAttributeSourceText, options));

                var thisClassAttributeSymbol = compilation.GetTypeByMetadataName($"ThisClassAttribute");
                if (thisClassAttributeSymbol is null)
                {
                    return;
                }

                var classLoggerAttributeSymbol = compilation.GetTypeByMetadataName($"ClassLoggerAttribute");
                if (classLoggerAttributeSymbol is null)
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

                    bool hasThisClassAttribute = ThisClassGenerator.HasAttribute(namedTypeSymbol, thisClassAttributeSymbol);
                    if (ThisClassGenerator.HasAttribute(namedTypeSymbol, classLoggerAttributeSymbol))
                    {
                        //ThisClassGenerator.AddThisClassToClass(context, namedTypeSymbol);
                        var classLoggerSource = GetCurrentClassLoggerSource(namedTypeSymbol);
                        hasThisClassAttribute = true;
                        if (classLoggerSource is not null)
                        {
                            ThisClassGenerator.AddThisClassToClass(context, namedTypeSymbol);
                            context.AddSource($"{namedTypeSymbol.Name}_ClassLogger.cs", SourceText.From(classLoggerSource, Encoding.UTF8));
                        }
                    }

                    if (hasThisClassAttribute)
                    {
                        var thisClassImplSource = GetIThisClassImplSource(namedTypeSymbol);
                        if (thisClassImplSource is not null)
                        {
                            context.AddSource($"{namedTypeSymbol.Name}_IThisClass.cs", SourceText.From(thisClassImplSource, Encoding.UTF8));
                        }
                    }
                }
            }
        }

        private static string? GetIThisClassImplSource(INamedTypeSymbol namedTypeSymbol)
        {
            if (namedTypeSymbol.IsInContainingNamespace())
            {
                var namespaceName = namedTypeSymbol.ContainingNamespace.ToDisplayString();
                var classNameFormat = new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeTypeConstraints | SymbolDisplayGenericsOptions.IncludeVariance);
                var className = namedTypeSymbol.ToDisplayString(classNameFormat);

                var thisClassContent = IThisClassImplTemplate.Render(new
                {
                    Namespace = namespaceName,
                    Class = className,
                });

                return thisClassContent;
            }

            return null;
        }

        private static string? GetCurrentClassLoggerSource(INamedTypeSymbol namedTypeSymbol)
        {
            if (namedTypeSymbol.IsInContainingNamespace())
            {
                var namespaceName = namedTypeSymbol.ContainingNamespace.ToDisplayString();
                var classNameFormat = new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeTypeConstraints | SymbolDisplayGenericsOptions.IncludeVariance);
                var className = namedTypeSymbol.ToDisplayString(classNameFormat);

                var thisClassContent = ClassLoggerTemplate.Render(new
                {
                    Namespace = namespaceName,
                    Class = className,
                });

                return thisClassContent;
            }

            return null;
        }

        class IThisClassSyntaxReceiver : ISyntaxReceiver
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
