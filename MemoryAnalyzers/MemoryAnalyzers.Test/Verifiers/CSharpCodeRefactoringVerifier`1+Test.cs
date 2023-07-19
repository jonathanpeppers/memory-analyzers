using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace MemoryAnalyzers.Test
{
	public static partial class CSharpCodeRefactoringVerifier<TCodeRefactoring>
		where TCodeRefactoring : CodeRefactoringProvider, new()
	{
		public class Test : CSharpCodeRefactoringTest<TCodeRefactoring, MSTestVerifier>
		{
			public Test()
			{
				SolutionTransforms.Add((solution, projectId) =>
				{
					var compilationOptions = solution.GetProject(projectId)?.CompilationOptions;
					if (compilationOptions is null)
						return solution;

					compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(
						compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings));
					solution = solution.WithProjectCompilationOptions(projectId, compilationOptions);

					return solution;
				});
			}
		}
	}
}
