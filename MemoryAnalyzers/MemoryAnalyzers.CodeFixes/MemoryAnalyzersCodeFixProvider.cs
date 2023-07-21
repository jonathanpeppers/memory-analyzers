using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace MemoryAnalyzers
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MemoryAnalyzersCodeFixProvider)), Shared]
	public class MemoryAnalyzersCodeFixProvider : CodeFixProvider
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create(MemoryAnalyzer.MA0001); }
		}

		public sealed override FixAllProvider GetFixAllProvider()
		{
			// See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
			return WellKnownFixAllProviders.BatchFixer;
		}

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
			if (root is null)
				return;

			// TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
			var diagnostic = context.Diagnostics.First();
			var diagnosticSpan = diagnostic.Location.SourceSpan;

			// Find the type declaration identified by the diagnostic.
			var parent = root.FindToken(diagnosticSpan.Start).Parent;
			if (parent is null)
				return;

			var declaration = parent.AncestorsAndSelf().OfType<EventFieldDeclarationSyntax>().First();

			// Register a code action that will invoke the fix.
			context.RegisterCodeFix(
				CodeAction.Create(
					title: CodeFixResources.MA0001Title,
					createChangedSolution: c => MakeUppercaseAsync(context.Document, declaration, c),
					equivalenceKey: nameof(CodeFixResources.MA0001Title)),
				diagnostic);
		}

		async Task<Solution> MakeUppercaseAsync(Document document, EventFieldDeclarationSyntax @event, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken);
			if (root is null)
				return document.Project.Solution;

			var attributes = @event.AttributeLists.Add(
				AttributeList(SingletonSeparatedList(
					Attribute(IdentifierName("MemoryLeakSafe"))
						.WithArgumentList(AttributeArgumentList(
								SingletonSeparatedList(
									AttributeArgument(
										LiteralExpression(
											SyntaxKind.StringLiteralExpression,
											Literal("Proven safe in test: XYZ"))))))
				)));

			return document.WithSyntaxRoot(
				root.ReplaceNode(
					@event,
					@event.WithAttributeLists(attributes).NormalizeWhitespace()
				)).Project.Solution;
		}
	}
}
