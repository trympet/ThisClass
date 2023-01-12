using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Scriban;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ThisClass.Common;

namespace ThisClass
{
    [Generator(LanguageNames.CSharp)]
    public partial class ThisClassGenerator : IIncrementalGenerator
    {
        internal static readonly SourceText ThisClassAttributeSourceText = SourceText.From(EmbeddedResource.GetContent("ThisClassAttribute.txt"), Encoding.UTF8);
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(context =>
            {
                context.AddSource("ThisClassAttribute.g", ThisClassAttributeSourceText);
            });

            context.RegisterSourceOutput(
                context.SyntaxProvider.ForAttributeWithMetadataName("ThisClassAttribute", IsClassDeclaration, static (ctx, ct) =>
                    (ctx.TargetSymbol.Name, AddThisClass(ThisClassContext.FromTypeSymbol(ctx.TargetNode, (INamedTypeSymbol)ctx.TargetSymbol, ctx.SemanticModel))
                        .CreateSourceText())
                ),
                static (ctx, source) => ctx.AddSource($"{source.Name}_ThisClass.g", source.Item2));
        }
    }
}
