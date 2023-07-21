using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MemoryAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class MemoryAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticId = "MemoryAnalyzers";
		private const string Category = "Memory";

		static readonly DiagnosticDescriptor EventRule = new(
			"MA0001",
			new LocalizableResourceString(nameof(Resources.MA0001Title),
			Resources.ResourceManager, typeof(Resources)),
			new LocalizableResourceString(nameof(Resources.MA0001MessageFormat),
			Resources.ResourceManager, typeof(Resources)),
			Category,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description: new LocalizableResourceString(nameof(Resources.MA0001Description), Resources.ResourceManager, typeof(Resources))
		);

		static readonly DiagnosticDescriptor FieldRule = new(
			"MA0002",
			new LocalizableResourceString(nameof(Resources.MA0002Title),
			Resources.ResourceManager, typeof(Resources)),
			new LocalizableResourceString(nameof(Resources.MA0002MessageFormat),
			Resources.ResourceManager, typeof(Resources)),
			Category,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description: new LocalizableResourceString(nameof(Resources.MA0002Description), Resources.ResourceManager, typeof(Resources))
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(EventRule, FieldRule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			// TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
			// See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
			context.RegisterSymbolAction(AnalyzeEvent, SymbolKind.Event);
			context.RegisterSyntaxNodeAction(AnalyzeField, SyntaxKind.FieldDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
		}

		static void AnalyzeEvent(SymbolAnalysisContext context)
		{
			var symbol = context.Symbol;
			if (!IsFromNSObjectSubclass(symbol.ContainingType))
				return;
			if (HasMemoryLeakSafeAttribute(symbol))
				return;

			context.ReportDiagnostic(Diagnostic.Create(EventRule, symbol.Locations[0], symbol.Name));
		}

		static void AnalyzeField(SyntaxNodeAnalysisContext context)
		{
			if (context.ContainingSymbol is not IFieldSymbol symbol || !IsFromNSObjectSubclass(symbol.ContainingType))
				return;
			if (HasMemoryLeakSafeAttribute(symbol))
				return;
			if (symbol.Type.Name == "WeakReference" ||
				symbol.Type.Name.StartsWith("WeakReference<", StringComparison.Ordinal))
				return;
			if (symbol.Type.IsValueType)
				return;

			context.ReportDiagnostic(Diagnostic.Create(FieldRule, symbol.Locations[0], symbol.Name));
		}

		static void AnalyzeProperty(SyntaxNodeAnalysisContext context)
		{
			if (context.ContainingSymbol is not IPropertySymbol symbol || !IsFromNSObjectSubclass(symbol.ContainingType))
				return;
			if (HasMemoryLeakSafeAttribute(symbol))
				return;
			if (symbol.Type.Name == "WeakReference" ||
				symbol.Type.Name.StartsWith("WeakReference<", StringComparison.Ordinal))
				return;
			if (symbol.Type.IsValueType)
				return;

			context.ReportDiagnostic(Diagnostic.Create(FieldRule, symbol.Locations[0], symbol.Name));
		}

		static bool HasMemoryLeakSafeAttribute(ISymbol symbol)
		{
			foreach (var attribute in symbol.GetAttributes())
			{
				if (attribute.AttributeClass?.Name == "MemoryLeakSafeAttribute")
					return true;
			}
			return false;
		}

		static bool IsFromNSObjectSubclass(INamedTypeSymbol type)
		{
			foreach (var attribute in type.GetAttributes())
			{
				if (attribute.AttributeClass is null)
					continue;
				if (attribute.AttributeClass.Name != "RegisterAttribute")
					continue;

				var ctorArgs = attribute.ConstructorArguments;
				if (ctorArgs.Length == 2)
				{
					if (ctorArgs[1].Value is bool parameterValue && parameterValue)
						return true;
				}

				var namedArgs = attribute.NamedArguments.FirstOrDefault(n => n.Key == "IsWrapper");
				if (namedArgs.Value.Value is bool namedValue && namedValue)
					return true;
			}

			var baseType = type.BaseType;
			if (baseType is null)
				return false;

			return IsFromNSObjectSubclass(baseType);
		}
	}
}
