using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ThisClass;
using ThisClass.Common;

namespace NLog.Extensions.ThisClass;


[Generator(LanguageNames.CSharp)]
internal class ThisClassNLogGenerator : IIncrementalGenerator
{
    private static readonly SourceText ClassLoggerAttributeSourceText = SourceText.From(EmbeddedResource.GetContent("ClassLoggerAttribute.txt"), Encoding.UTF8);
    private static readonly SourceText ClassLoggerLazyAttributeSourceText = SourceText.From(EmbeddedResource.GetContent("ClassLoggerLazyAttribute.txt"), Encoding.UTF8);
    private static readonly MemberDeclarationSyntax[] ClassLoggerMembers = new[]
    {
        SyntaxFactory.ParseMemberDeclaration(
        "private static readonly global::NLog.Logger Logger = global::NLog.LogManager.GetLogger(ThisClass.FullName);")!
    };
    private static readonly MemberDeclarationSyntax[] ClassLoggerLazyMembers = new[]
    {
        SyntaxFactory.ParseMemberDeclaration("private static global::NLog.Logger? __loggerLazy;")!,
        SyntaxFactory.ParseMemberDeclaration("private static global::NLog.Logger Logger => __loggerLazy ??= global::NLog.LogManager.GetLogger(ThisClass.FullName);")!
    };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(context =>
        {
            context.AddSource("ClassLoggerAttribute.g", ClassLoggerAttributeSourceText);
            context.AddSource("ClassLoggerLazyAttribute.g", ClassLoggerLazyAttributeSourceText);
        });

        context.RegisterSourceOutput(
            context.SyntaxProvider.ForAttributeWithMetadataName("ClassLoggerAttribute", ThisClassGenerator.IsClassDeclaration, TransformClassLogger),
            AddSource);
        context.RegisterSourceOutput(
            context.SyntaxProvider.ForAttributeWithMetadataName("ClassLoggerLazyAttribute", ThisClassGenerator.IsClassDeclaration, TransformClassLoggerLazy),
            AddSource);
    }

    public KeyValuePair<string, SourceText> TransformClassLogger(GeneratorAttributeSyntaxContext syntaxContext, CancellationToken cancellationToken) =>
        new($"{syntaxContext.TargetSymbol.Name}_ClassLogger", Transform(syntaxContext, ClassLoggerMembers));

    public KeyValuePair<string, SourceText> TransformClassLoggerLazy(GeneratorAttributeSyntaxContext syntaxContext, CancellationToken cancellationToken) =>
        new($"{syntaxContext.TargetSymbol.Name}_ClassLoggerLazy", Transform(syntaxContext, ClassLoggerLazyMembers));

    public static SourceText Transform(GeneratorAttributeSyntaxContext syntaxContext, MemberDeclarationSyntax[] members)
    {
        var namedTypeSymbol = (INamedTypeSymbol)syntaxContext.TargetSymbol;
        try
        {
            var context = ThisClassContext.FromTypeSymbol(syntaxContext.TargetNode, namedTypeSymbol, syntaxContext.SemanticModel);
            context = ThisClassGenerator.AddThisClass(context);
            context = context with { Members = context.Members.AddRange(members) };
            return context.CreateSourceText();

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

    public static void AddSource(SourceProductionContext context, KeyValuePair<string, SourceText> source)
    {
        context.AddSource($"{source.Key}.g", source.Value);
    }
}
