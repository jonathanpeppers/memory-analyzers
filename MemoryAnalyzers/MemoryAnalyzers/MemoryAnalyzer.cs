using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MemoryAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class MemoryAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticId = "MemoryAnalyzers";

		// You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
		// See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
		private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
		private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
		private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
		private const string Category = "Memory";

		private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor("MA0001", Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			// TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
			// See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
			context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Event);
		}

		private static void AnalyzeSymbol(SymbolAnalysisContext context)
		{
			var symbol = context.Symbol;

			foreach (var attribute in symbol.GetAttributes())
			{
				// If we are marked with [SafeEvent] return
				if (attribute.AttributeClass.Name == "SafeEventAttribute")
					return;
			}

			var diagnostic = Diagnostic.Create(Rule, symbol.Locations[0], symbol.Name);
			context.ReportDiagnostic(diagnostic);
		}
	}
}
