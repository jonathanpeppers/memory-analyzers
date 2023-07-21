using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = MemoryAnalyzers.Test.CSharpCodeFixVerifier<
	MemoryAnalyzers.MemoryAnalyzer,
	MemoryAnalyzers.MemoryAnalyzersCodeFixProvider>;

namespace MemoryAnalyzers.Test
{
	[TestClass]
	public class CodeFixUnitTests
	{
		[TestMethod]
		public async Task CodeFix()
		{
			var test = """
			class Foo : NSObject
			{
			    public event EventHandler {|#0:EventName|};
			}
			""";

			var codefix = """
			class Foo : NSObject
			{
			    [MemoryLeakSafe("Proven safe in test: XYZ")]
			    public event EventHandler {|#0:EventName|};
			}
			""";
			;

			var expected = VerifyCS.Diagnostic("MA0001").WithLocation(0).WithArguments("EventName");
			await VerifyCS.VerifyCodeFixAsync(test, expected, codefix);
		}
	}
}
