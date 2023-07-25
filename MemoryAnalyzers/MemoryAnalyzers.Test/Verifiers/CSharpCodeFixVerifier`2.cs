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

			// Intentionally left out of codefix tests
			test.TestState.Sources.Add("global using System.Diagnostics.CodeAnalysis;");

			test.ExpectedDiagnostics.AddRange(expected);
			await test.RunAsync(CancellationToken.None);
		}

		/// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyCodeFixAsync(string, string)"/>
		public static async Task VerifyCodeFixAsync(string source, string fixedSource, int index)
			=> await VerifyCodeFixAsync(source, DiagnosticResult.EmptyDiagnosticResults, fixedSource, index);

		/// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyCodeFixAsync(string, DiagnosticResult, string)"/>
		public static async Task VerifyCodeFixAsync(string source, DiagnosticResult expected, string fixedSource, int index, int iterations = 1)
			=> await VerifyCodeFixAsync(source, new[] { expected }, fixedSource, index, iterations);

		/// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyCodeFixAsync(string, DiagnosticResult[], string)"/>
		public static async Task VerifyCodeFixAsync(string source, DiagnosticResult[] expected, string fixedSource, int index, int iterations = 1)
		{
			var test = new Test
			{
				TestCode = source,
				FixedCode = fixedSource,
				CodeActionIndex = index,
				NumberOfIncrementalIterations = iterations,
				NumberOfFixAllInDocumentIterations = iterations,
				NumberOfFixAllInProjectIterations = iterations,
				NumberOfFixAllIterations = iterations,
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
			testState.Sources.Add("global using CoreAnimation;");
			testState.Sources.Add("global using Foundation;");
			testState.Sources.Add("global using UIKit;");

			// [UnconditionalSuppressMessage]
			testState.Sources.Add("""
				namespace System.Diagnostics.CodeAnalysis;

				[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
				sealed class UnconditionalSuppressMessageAttribute : Attribute
				{
					public UnconditionalSuppressMessageAttribute(string category, string checkId)
					{
						Category = category;
						CheckId = checkId;
					}

					public string Category { get; }

					public string CheckId { get; }

					public string Justification { get; set; }
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

			// UIKit.UIColor
			testState.Sources.Add("""
				namespace UIKit;

				[Register("UIColor", true)]
				class UIColor : NSObject { }
			""");

			// CoreAnimation.CALayer
			testState.Sources.Add("""
				namespace CoreAnimation;

				[Register("CALayer", true)]
				class CALayer : NSObject { }
			""");
		}
	}
}
