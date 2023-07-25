using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace MemoryAnalyzers;

static class Extensions
{
	public static IEnumerable<INamedTypeSymbol> IterateBaseTypes(this ITypeSymbol type)
	{
		var baseType = type.BaseType;
		while (baseType != null)
		{
			yield return baseType;

			baseType = baseType.BaseType;
		}
	}
}
