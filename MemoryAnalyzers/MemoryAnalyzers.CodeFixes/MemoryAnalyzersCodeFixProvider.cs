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
			get { return ImmutableArray.Create(MemoryAnalyzer.MA0001, MemoryAnalyzer.MA0002); }
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
			var diagnostic = context.Diagnostics.First();
			var diagnosticSpan = diagnostic.Location.SourceSpan;
			var parent = root.FindToken(diagnosticSpan.Start).Parent;
			if (parent is null)
				return;

			if (diagnostic.Id == MemoryAnalyzer.MA0001)
			{
				var declaration = parent.AncestorsAndSelf().OfType<EventFieldDeclarationSyntax>().First();
				context.RegisterCodeFix(
					CodeAction.Create(
						title: CodeFixResources.RemoveMember,
						createChangedSolution: c => RemoveMember(context.Document, declaration, c),
						equivalenceKey: nameof(CodeFixResources.RemoveMember)),
					diagnostic);
				context.RegisterCodeFix(
					CodeAction.Create(
						title: CodeFixResources.AddMemoryLeakSafe,
						createChangedSolution: c => AddMemorySafeAttribute(context.Document, declaration, c),
						equivalenceKey: nameof(CodeFixResources.AddMemoryLeakSafe)),
					diagnostic);
			}
			else if (diagnostic.Id == MemoryAnalyzer.MA0002)
			{
				var declaration = parent.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().First();
				context.RegisterCodeFix(
					CodeAction.Create(
						title: CodeFixResources.RemoveMember,
						createChangedSolution: c => RemoveMember(context.Document, declaration, c),
						equivalenceKey: nameof(CodeFixResources.RemoveMember)),
					diagnostic);
				context.RegisterCodeFix(
					CodeAction.Create(
						title: CodeFixResources.AddMemoryLeakSafe,
						createChangedSolution: c => AddMemorySafeAttribute(context.Document, declaration, c),
						equivalenceKey: nameof(CodeFixResources.AddMemoryLeakSafe)),
					diagnostic);
			}
		}

		async Task<Solution> RemoveMember(Document document, MemberDeclarationSyntax member, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken);
			if (root is null)
				return document.Project.Solution;
			return document.WithSyntaxRoot(root.RemoveNode(member, SyntaxRemoveOptions.KeepLeadingTrivia | SyntaxRemoveOptions.KeepTrailingTrivia)!).Project.Solution;
		}

		async Task<Solution> AddMemorySafeAttribute(Document document, MemberDeclarationSyntax member, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken);
			if (root is null || member.Parent is null)
				return document.Project.Solution;

			// Used: http://roslynquoter.azurewebsites.net/
			var attributes = member.AttributeLists.Add(
				AttributeList(SingletonSeparatedList(
					Attribute(IdentifierName("MemoryLeakSafe"))
						.WithArgumentList(
							AttributeArgumentList(
								SingletonSeparatedList(
									AttributeArgument(
										LiteralExpression(
											SyntaxKind.StringLiteralExpression,
											Literal("Proven safe in test: XYZ"))))))
				)));

			return document.WithSyntaxRoot(
					root.ReplaceNode(member, member.WithAttributeLists(attributes)))
				.Project.Solution;
		}
	}
}
