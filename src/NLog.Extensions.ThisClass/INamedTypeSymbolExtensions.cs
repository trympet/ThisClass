using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace ThisClass
{
    internal static class INamedTypeSymbolExtensions
    {
        public static bool IsInContainingNamespace(this INamedTypeSymbol namedTypeSymbol)
            => namedTypeSymbol.ContainingSymbol.Equals(namedTypeSymbol.ContainingNamespace,
                SymbolEqualityComparer.Default);
    }
}
