using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MemoryAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class MemoryAnalyzer : DiagnosticAnalyzer
	{
		public const string MA0001 = "MA0001";
		public const string MA0002 = "MA0002";
		public const string MA0003 = "MA0003";
		const string Category = "Memory";

		/// <summary>
		/// List of types that are "generally OK"
		/// </summary>
		static readonly HashSet<(string Namespace, string Name)> GenerallySafe = new()
		{
			// UIColor is fine
			{ ("UIKit", "UIColor") },
			// UIImage is fine
			{ ("UIKit", "UIImage") },
		};

		/// <summary>
		/// List of types when inside other types, that are "generally OK"
		/// </summary>
		static readonly Dictionary<(string Namespace, string Name), (string Namespace, string Name)> SafeWhenInsideOf = new ()
		{
			// CALayer in a UIView is fine
			{ ("UIKit", "UIView"), ("CoreAnimation", "CALayer") },
			// UIWindow in a UIApplicationDelegate/IUIApplicationDelegate is fine
			{ ("UIKit", "UIApplicationDelegate"), ("UIKit", "UIWindow") },
			{ ("UIKit", "IUIApplicationDelegate"), ("UIKit", "UIWindow") },
			{ ("UIKit", "IUIWindowSceneDelegate"), ("UIKit", "UIWindow") },
		};

		static readonly DiagnosticDescriptor MA0001Rule = new(
			MA0001,
			new LocalizableResourceString(nameof(Resources.MA0001Title),
			Resources.ResourceManager, typeof(Resources)),
			new LocalizableResourceString(nameof(Resources.MA0001MessageFormat),
			Resources.ResourceManager, typeof(Resources)),
			Category,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description: new LocalizableResourceString(nameof(Resources.MA0001Description), Resources.ResourceManager, typeof(Resources))
		);

		static readonly DiagnosticDescriptor MA0002Rule = new(
			MA0002,
			new LocalizableResourceString(nameof(Resources.MA0002Title),
			Resources.ResourceManager, typeof(Resources)),
			new LocalizableResourceString(nameof(Resources.MA0002MessageFormat),
			Resources.ResourceManager, typeof(Resources)),
			Category,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description: new LocalizableResourceString(nameof(Resources.MA0002Description), Resources.ResourceManager, typeof(Resources))
		);

		static readonly DiagnosticDescriptor MA0003Rule = new(
			MA0003,
			new LocalizableResourceString(nameof(Resources.MA0003Title),
			Resources.ResourceManager, typeof(Resources)),
			new LocalizableResourceString(nameof(Resources.MA0003MessageFormat),
			Resources.ResourceManager, typeof(Resources)),
			Category,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description: new LocalizableResourceString(nameof(Resources.MA0003Description), Resources.ResourceManager, typeof(Resources))
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(MA0001Rule, MA0002Rule, MA0003Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			// TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
			// See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
			context.RegisterSymbolAction(AnalyzeEvent, SymbolKind.Event);
			context.RegisterSyntaxNodeAction(AnalyzeField, SyntaxKind.FieldDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzeSubscription, SyntaxKind.AddAssignmentExpression);
		}

		static void AnalyzeEvent(SymbolAnalysisContext context)
		{
			if (context.Symbol is not IEventSymbol symbol)
				return;
			if (symbol.DeclaredAccessibility == Accessibility.Private)
				return;
			if (!IsNSObjectSubclass(symbol.ContainingType))
				return;
			if (HasUnconditionalSuppressMessage(symbol, MA0001))
				return;
			if (symbol.AddMethod is null)
				return;

			// Return if we find an `add { }` implementation at all
			foreach (var syntaxReference in symbol.AddMethod.DeclaringSyntaxReferences)
			{
				var node = syntaxReference.GetSyntax();
				if (node is AccessorDeclarationSyntax)
					return;
			}

			context.ReportDiagnostic(Diagnostic.Create(MA0001Rule, symbol.Locations[0], symbol.Name));
		}

		static void AnalyzeField(SyntaxNodeAnalysisContext context)
		{
			if (context.ContainingSymbol is not IFieldSymbol symbol || !IsNSObjectSubclass(symbol.ContainingType))
				return;
			if (HasUnconditionalSuppressMessage(symbol, MA0002))
				return;
			if (symbol.Type.IsValueType)
				return;
			if (symbol.Type is INamedTypeSymbol namedSymbol)
			{
				if (GenerallySafe.Contains((namedSymbol.ContainingNamespace.Name, namedSymbol.Name)))
					return;
				if (!IsObject(namedSymbol) && !IsDelegateType(namedSymbol) && !IsNSObjectSubclass(namedSymbol))
					return;
				if (IsGenerallySafe(symbol.ContainingType, symbol.Type))
					return;
			}
			else if (symbol.Type is IArrayTypeSymbol arraySymbol)
			{
				if (arraySymbol.ElementType.IsValueType)
					return;
			}

			context.ReportDiagnostic(Diagnostic.Create(MA0002Rule, symbol.Locations[0], symbol.Name));
		}

		static void AnalyzeProperty(SyntaxNodeAnalysisContext context)
		{
			if (context.ContainingSymbol is not IPropertySymbol symbol || !IsNSObjectSubclass(symbol.ContainingType))
				return;
			if (HasUnconditionalSuppressMessage(symbol, MA0002))
				return;
			if (symbol.Type.IsValueType)
				return;
			if (symbol.Type is INamedTypeSymbol namedSymbol)
			{
				if (GenerallySafe.Contains((namedSymbol.ContainingNamespace.Name, namedSymbol.Name)))
					return;
				if (!IsObject(namedSymbol) && !IsDelegateType(namedSymbol) && !IsNSObjectSubclass(namedSymbol))
					return;
				if (IsGenerallySafe(symbol.ContainingType, symbol.Type))
					return;
			}
			else if (symbol.Type is IArrayTypeSymbol arraySymbol)
			{
				if (arraySymbol.ElementType.IsValueType)
					return;
			}
			if (!IsAutoProperty(symbol))
				return;

			context.ReportDiagnostic(Diagnostic.Create(MA0002Rule, symbol.Locations[0], symbol.Name));
		}

		static void AnalyzeSubscription(SyntaxNodeAnalysisContext context)
		{
			if (context.ContainingSymbol is not ISymbol symbol || !IsNSObjectSubclass(symbol.ContainingType))
				return;
			if (context.Node is not AssignmentExpressionSyntax assignment)
				return;
			var rightInfo = context.SemanticModel.GetSymbolInfo(assignment.Right);
			if (rightInfo.Symbol is not IMethodSymbol methodSymbol || methodSymbol.IsStatic)
				return; // static methods are fine
			if (!IsNSObjectSubclass(methodSymbol.ContainingType))
				return; // If the method is on a non-NSObject subclass, it's fine

			// Subscribing to events you declare are fine
			if (assignment.Left is IdentifierNameSyntax)
				return;
			if (assignment.Left is MemberAccessExpressionSyntax left && left.Expression is ThisExpressionSyntax)
				return;

			// We have to calculate the proper location for something like `+= _proxy.OnValueChanged`
			Location? location;
			if (assignment.Right is MemberAccessExpressionSyntax right)
				location = right.Name.GetLocation();
			else
				location = assignment.Right.GetLocation();

			context.ReportDiagnostic(Diagnostic.Create(MA0003Rule, location, methodSymbol.Name));
		}

		static bool HasUnconditionalSuppressMessage(ISymbol symbol, string expectedCode)
		{
			foreach (var attribute in symbol.GetAttributes())
			{
				if (attribute.AttributeClass is null)
					continue;
				if (attribute.AttributeClass.ContainingNamespace.Name != "System.Diagnostics.CodeAnalysis")
					continue;
				if (attribute.AttributeClass.Name != "UnconditionalSuppressMessageAttribute")
					continue;

				var ctorArgs = attribute.ConstructorArguments;
				if (ctorArgs.Length == 2)
				{
					return ctorArgs[1].Value as string == expectedCode;
				}

				// This only has a single 2-argument constructor, but let's keep this logic in case it ever changes
				var namedArgs = attribute.NamedArguments.FirstOrDefault(n => n.Key == "CheckId");
				return namedArgs.Value.Value as string == expectedCode;
			}
			return false;
		}

		/// <summary>
		/// Returns true if we found these in a known list of OK types
		/// NOTE: can be O(N^2) time, iterates base types & interfaces and base types of member type
		/// </summary>
		static bool IsGenerallySafe(INamedTypeSymbol containingType, ITypeSymbol memberType)
		{
			foreach (var iface in containingType.AllInterfaces)
			{
				if (SafeWhenInsideOf.TryGetValue((iface.ContainingNamespace.Name, iface.Name), out var safeMember) &&
					safeMember.Namespace == memberType.ContainingNamespace.Name && safeMember.Name == memberType.Name)
				{
					return true;
				}
			}

			foreach (var containingBaseType in containingType.IterateBaseTypes())
			{
				if (SafeWhenInsideOf.TryGetValue((containingBaseType.ContainingNamespace.Name, containingBaseType.Name), out var safeMember))
				{
					if (safeMember.Namespace == memberType.ContainingNamespace.Name && safeMember.Name == memberType.Name)
						return true;

					foreach (var baseMemberType in memberType.IterateBaseTypes())
					{
						if (safeMember.Namespace == baseMemberType.ContainingNamespace.Name && safeMember.Name == baseMemberType.Name)
							return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Returns true if a type is a special NSObject type
		/// NOTE: can be O(N) time, recurses base types
		/// </summary>
		static bool IsNSObjectSubclass(INamedTypeSymbol type)
		{
			foreach (var attribute in type.GetAttributes())
			{
				if (attribute.AttributeClass is null)
					continue;
				if (attribute.AttributeClass.ContainingNamespace.Name != "Foundation")
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

			return IsNSObjectSubclass(baseType);
		}

		/// <summary>
		/// Returns true if a type is a System.Delegate
		/// NOTE: can be O(N) time, recurses base types
		/// </summary>
		static bool IsDelegateType(INamedTypeSymbol type)
		{
			if (type.ContainingNamespace.Name == "System" && type.Name == "Delegate")
				return true;

			var baseType = type.BaseType;
			if (baseType is null)
				return false;

			return IsDelegateType(baseType);
		}

		/// <summary>
		/// Returns true if a type is exactly System.Object
		/// </summary>
		static bool IsObject(INamedTypeSymbol type) =>
			type.ContainingNamespace.Name == "System" && type.Name == "Object";

		//Swiped from: https://www.meziantou.net/checking-if-a-property-is-an-auto-implemented-property-in-roslyn.htm
		static bool IsAutoProperty(IPropertySymbol propertySymbol)
		{
			// Get fields declared in the same type as the property
			var fields = propertySymbol.ContainingType.GetMembers().OfType<IFieldSymbol>();

			// Check if one field is associated to
			return fields.Any(field => SymbolEqualityComparer.Default.Equals(field.AssociatedSymbol, propertySymbol));
		}
	}
}
