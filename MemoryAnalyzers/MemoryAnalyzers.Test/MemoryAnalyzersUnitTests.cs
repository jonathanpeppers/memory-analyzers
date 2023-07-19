using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = MemoryAnalyzers.Test.CSharpCodeFixVerifier<
	MemoryAnalyzers.MemoryAnalyzer,
	MemoryAnalyzers.MemoryAnalyzersCodeFixProvider>;

namespace MemoryAnalyzers.Test
{
	[TestClass]
	public class MemoryAnalyzersUnitTest
	{
		[TestMethod]
		public async Task NoSource()
		{
			await VerifyCS.VerifyAnalyzerAsync("");
		}

		//Diagnostic and CodeFix both triggered and checked for
		[TestMethod]
		[Ignore("Code fix not implemented yet")]
		public async Task CodeFix()
		{
			var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class {|#0:TypeName|}
        {   
        }
    }";

			var fixtest = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class TYPENAME
        {   
        }
    }";

			var expected = VerifyCS.Diagnostic("MA0001").WithLocation(0).WithArguments("TypeName");
			await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
		}

		[TestMethod]
		public async Task EventHandler()
		{
			var test = """
                namespace ConsoleApplication1;

                class Foo
                {   
                    public event EventHandler {|#0:EventName|};
                }
            """;

			var expected = VerifyCS.Diagnostic("MA0001").WithLocation(0).WithArguments("EventName");
			await VerifyCS.VerifyAnalyzerAsync(test, expected);
		}

		[TestMethod]
		public async Task SafeEventHandler()
		{
			var test = """
                namespace ConsoleApplication1;

                class Foo
                {
                    [SafeEvent("Event tested via MemoryTests.MyEvent")]
                    public event EventHandler {|#0:EventName|};
                }
            """;

			// 0 warnings
			await VerifyCS.VerifyAnalyzerAsync(test);
		}
	}
}
