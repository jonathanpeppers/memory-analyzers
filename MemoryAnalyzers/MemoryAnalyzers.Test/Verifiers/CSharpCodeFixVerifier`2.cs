using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace MemoryAnalyzers.Test
{
	public static partial class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
		where TAnalyzer : DiagnosticAnalyzer, new()
		where TCodeFix : CodeFixProvider, new()
	{
		/// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.Diagnostic()"/>
		public static DiagnosticResult Diagnostic()
			=> CSharpCodeFixVerifier<TAnalyzer, TCodeFix, MSTestVerifier>.Diagnostic();

		/// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.Diagnostic(string)"/>
		public static DiagnosticResult Diagnostic(string diagnosticId)
			=> CSharpCodeFixVerifier<TAnalyzer, TCodeFix, MSTestVerifier>.Diagnostic(diagnosticId);

		/// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.Diagnostic(DiagnosticDescriptor)"/>
		public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
			=> CSharpCodeFixVerifier<TAnalyzer, TCodeFix, MSTestVerifier>.Diagnostic(descriptor);

		/// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyAnalyzerAsync(string, DiagnosticResult[])"/>
		public static async Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
		{
			var test = new Test
			{
				TestCode = source,
			};

			AddTestCode(test.TestState);

			test.ExpectedDiagnostics.AddRange(expected);
			await test.RunAsync(CancellationToken.None);
		}

		/// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyCodeFixAsync(string, string)"/>
		public static async Task VerifyCodeFixAsync(string source, string fixedSource, int index)
			=> await VerifyCodeFixAsync(source, DiagnosticResult.EmptyDiagnosticResults, fixedSource, index);

		/// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyCodeFixAsync(string, DiagnosticResult, string)"/>
		public static async Task VerifyCodeFixAsync(string source, DiagnosticResult expected, string fixedSource, int index)
			=> await VerifyCodeFixAsync(source, new[] { expected }, fixedSource, index);

		/// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyCodeFixAsync(string, DiagnosticResult[], string)"/>
		public static async Task VerifyCodeFixAsync(string source, DiagnosticResult[] expected, string fixedSource, int index)
		{
			var test = new Test
			{
				TestCode = source,
				FixedCode = fixedSource,
				CodeActionIndex = index,
			};

			AddTestCode(test.TestState);
			AddTestCode(test.FixedState);

			test.ExpectedDiagnostics.AddRange(expected);
			await test.RunAsync(CancellationToken.None);
		}

		static void AddTestCode(SolutionState testState)
		{
			// Global usings
			testState.Sources.Add("global using System;");
			testState.Sources.Add("global using Foundation;");
			testState.Sources.Add("global using UIKit;");

			// [Safe*] attributes
			testState.Sources.Add("""
				[AttributeUsage(AttributeTargets.Event | AttributeTargets.Field | AttributeTargets.Property)]
				sealed class MemoryLeakSafeAttribute : Attribute
				{
					public MemoryLeakSafeAttribute(string justification)
					{
						Justification = justification;
					}

					public string Justification { get; private set; }
				}
			""");

			// Foundation.NSObject
			testState.Sources.Add("""
				namespace Foundation;

				[AttributeUsage(AttributeTargets.Class)]
				sealed class RegisterAttribute : Attribute
				{
					public string Name { get; set; }

					public bool IsWrapper { get; set; }

					public RegisterAttribute() { }

					public RegisterAttribute(string name, bool isWrapper)
					{
						Name = name;
						IsWrapper = isWrapper;
					}
				}

				[Register("NSObject", isWrapper: true)]
				class NSObject { }
			""");

			// UIKit.UIView
			testState.Sources.Add("""
				namespace UIKit;

				[Register("UIView", isWrapper: true)]
				class UIView : NSObject { }
			""");
		}
	}
}
