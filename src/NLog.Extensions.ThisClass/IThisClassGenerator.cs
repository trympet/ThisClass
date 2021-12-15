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
            context.AddSource("ThisClassAttribute_IThisClass.g", IThisClassSourceText);
            context.AddSource("ThisClassExtensions.g", ThisClassExtensionsSourceText);
            context.AddSource("ClassLoggerAttribute.g", ClassLoggerAttributeSourceText);

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

                var classLoggerAttributeSymbol = compilation.GetTypeByMetadataName("ClassLoggerAttribute");
                if (classLoggerAttributeSymbol is null)
                {
                    return;
                }

                var namedTypeSymbols = receiver.CandidateClasses
                    .Select(x => compilation
                        .GetSemanticModel(x.SyntaxTree)
                        .GetDeclaredSymbol(x))
                    .Distinct(SymbolEqualityComparer.Default)
                    .OfType<INamedTypeSymbol>();

                foreach (var namedTypeSymbol in namedTypeSymbols)
                {
                    try
                    {
                        bool hasThisClassAttribute = ThisClassGenerator.HasAttribute(namedTypeSymbol, thisClassAttributeSymbol);
                        if (ThisClassGenerator.HasAttribute(namedTypeSymbol, classLoggerAttributeSymbol))
                        {
                            //ThisClassGenerator.AddThisClassToClass(context, namedTypeSymbol);
                            var classLoggerSource = GetCurrentClassLoggerSource(namedTypeSymbol);
                            hasThisClassAttribute = true;
                            if (classLoggerSource is not null)
                            {
                                ThisClassGenerator.AddThisClassToClass(context, namedTypeSymbol);
                                context.AddSource($"{namedTypeSymbol.Name}_ClassLogger.g", SourceText.From(classLoggerSource, Encoding.UTF8));
                            }
                        }

                        if (hasThisClassAttribute)
                        {
                            var thisClassImplSource = GetIThisClassImplSource(namedTypeSymbol);
                            if (thisClassImplSource is not null)
                            {
                                context.AddSource($"{namedTypeSymbol.Name}_IThisClass.g", SourceText.From(thisClassImplSource, Encoding.UTF8));
                            }
                        }

                    }
                    catch (Exception e)
                    {
                        _ = e;
#if DEBUG
                        System.Diagnostics.Debugger.Launch();
#endif
                        throw;
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
                var parts = namedTypeSymbol.ToDisplayParts(classNameFormat);
                const string interfaceImpl = " : global::ThisClassAttribute.IThisClass";
                if (className.Contains(" where"))
                {
                    className = className.Insert(className.IndexOf(" where"), interfaceImpl);
                }
                else
                {
                    className += interfaceImpl;
                }

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
            public HashSet<ClassDeclarationSyntax> CandidateClasses { get; } = new();

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
