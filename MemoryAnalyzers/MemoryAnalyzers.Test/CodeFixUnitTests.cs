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
		public async Task MA0001_Remove()
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
			    
			}
			""";
			;

			var expected = VerifyCS.Diagnostic("MA0001").WithLocation(0).WithArguments("EventName");
			await VerifyCS.VerifyCodeFixAsync(test, expected, codefix, index: 0);
		}

		[TestMethod]
		public async Task MA0001_MemoryLeakSafe()
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
			await VerifyCS.VerifyCodeFixAsync(test, expected, codefix, index: 1);
		}

		[TestMethod]
		public async Task MA0002_Remove()
		{
			var test = """
			class Foo : NSObject
			{
			    public UIView {|#0:FieldName|};
			}
			""";

			var codefix = """
			class Foo : NSObject
			{
			    
			}
			""";
			;

			var expected = VerifyCS.Diagnostic("MA0002").WithLocation(0).WithArguments("FieldName");
			await VerifyCS.VerifyCodeFixAsync(test, expected, codefix, index: 0);
		}

		[TestMethod]
		public async Task MA0002_MemoryLeakSafe()
		{
			var test = """
			class Foo : NSObject
			{
			    public UIView {|#0:FieldName|};
			}
			""";

			var codefix = """
			class Foo : NSObject
			{
			    [MemoryLeakSafe("Proven safe in test: XYZ")]
			    public UIView {|#0:FieldName|};
			}
			""";
			;

			var expected = VerifyCS.Diagnostic("MA0002").WithLocation(0).WithArguments("FieldName");
			await VerifyCS.VerifyCodeFixAsync(test, expected, codefix, index: 1);
		}
	}
}
