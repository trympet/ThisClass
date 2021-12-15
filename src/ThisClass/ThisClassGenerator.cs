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
    public partial class ThisClassGenerator : ISourceGenerator
    {
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
