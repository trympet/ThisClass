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
    internal class ThisClassNLogGenerator : ISourceGenerator
    {
        private static readonly SourceText ClassLoggerAttributeSourceText = SourceText.From(EmbeddedResource.GetContent("ClassLoggerAttribute.txt"), Encoding.UTF8);
        private static readonly SourceText ClassLoggerLazyAttributeSourceText = SourceText.From(EmbeddedResource.GetContent("ClassLoggerLazyAttribute.txt"), Encoding.UTF8);
        private static readonly Template ClassLoggerTemplate = Template.Parse(EmbeddedResource.GetContent("ClassLogger.sbntxt"));
        private static readonly Template ClassLoggerLazyTemplate = Template.Parse(EmbeddedResource.GetContent("ClassLoggerLazy.sbntxt"));

        public void Initialize(GeneratorInitializationContext context)
        {
            //System.Diagnostics.Debugger.Launch();
            context.RegisterForSyntaxNotifications(() => new ThisClassNLogSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            //System.Diagnostics.Debugger.Launch();
            context.AddSource("ClassLoggerAttribute.g", ClassLoggerAttributeSourceText);
            context.AddSource("ClassLoggerLazyAttribute.g", ClassLoggerLazyAttributeSourceText);

            if (context.SyntaxReceiver is ThisClassNLogSyntaxReceiver receiver)
            {
                var options = (context.Compilation as CSharpCompilation)?.SyntaxTrees[0].Options as CSharpParseOptions;
                var compilation = context.Compilation.AddSyntaxTrees(
                    CSharpSyntaxTree.ParseText(ClassLoggerAttributeSourceText, options),
                    CSharpSyntaxTree.ParseText(ClassLoggerLazyAttributeSourceText, options));

                var classLoggerAttributeSymbol = compilation.GetTypeByMetadataName("ClassLoggerAttribute");
                if (classLoggerAttributeSymbol is null)
                {
                    return;
                }

                var classLoggerLazyAttributeSymbol = compilation.GetTypeByMetadataName("ClassLoggerLazyAttribute");
                if (classLoggerLazyAttributeSymbol is null)
                {
                    return;
                }

                var namedTypeSymbols = receiver.CandidateClasses
                    .Where(x => x.AttributeLists.Count > 0)
                    .Select(x => compilation
                        .GetSemanticModel(x.SyntaxTree)
                        .GetDeclaredSymbol(x))
                    .Distinct(SymbolEqualityComparer.Default)
                    .OfType<INamedTypeSymbol>();

                foreach (var namedTypeSymbol in namedTypeSymbols)
                {
                    try
                    {
                        var template = ThisClassGenerator.HasAttribute(namedTypeSymbol, classLoggerLazyAttributeSymbol)
                            ? ClassLoggerLazyTemplate
                            : ThisClassGenerator.HasAttribute(namedTypeSymbol, classLoggerAttributeSymbol)
                                ? ClassLoggerTemplate
                                : null;
                        var source = template is not null ? GetCurrentClassLoggerSource(namedTypeSymbol, template, compilation) : null;
                        if (source is not null)
                        {
                            ThisClassGenerator.AddThisClassToClass(context, namedTypeSymbol);
                            context.AddSource($"{namedTypeSymbol.Name}_ClassLogger.g", SourceText.From(source, Encoding.UTF8));
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

        private static string? GetCurrentClassLoggerSource(INamedTypeSymbol namedTypeSymbol, Template template, Compilation compilation)
        {
            if (namedTypeSymbol.IsInContainingNamespace())
            {
                var (namespaceName, className) = ThisClassGenerator.GetClassDeclaration(namedTypeSymbol, compilation);
                return template.Render(new
                {
                    Namespace = namespaceName,
                    Class = className,
                });
            }

            return null;
        }

        class ThisClassNLogSyntaxReceiver : ISyntaxReceiver
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
