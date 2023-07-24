using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MemoryAnalyzers;

static class Extensions
{
	// https://github.com/protobuf-net/protobuf-net/blob/ae84aba11942eb45d261316a966c811bac8565b3/src/protobuf-net.BuildTools/Internal/Roslyn/Extensions/CompilationUnitSyntaxExtensions.cs
	public static CompilationUnitSyntax AddUsingsIfNotExist(
		this CompilationUnitSyntax compilationUnitSyntax,
		params string[]? usingDirectiveNames)
	{
		if (usingDirectiveNames is null || usingDirectiveNames.Length == 0)
			return compilationUnitSyntax;

		// build a hashset for efficient lookup
		// comparison is done based on string value, because different usings can have different types of identifiers:
		// - IdentifierName
		// - QualifiedNameSyntax
		var existingUsingDirectiveNames = compilationUnitSyntax.Usings
			.Select(x => x.Name?.ToString().Trim())
			.ToImmutableHashSet();

		foreach (var directive in usingDirectiveNames)
		{
			var directiveTrimmed = directive.Trim();
			if (!existingUsingDirectiveNames.Contains(directiveTrimmed))
			{
				compilationUnitSyntax = compilationUnitSyntax.AddUsings(
					SyntaxFactory.UsingDirective(
						SyntaxFactory.ParseName(" " + directiveTrimmed)));
			}
		}

		return compilationUnitSyntax;
	}
}
