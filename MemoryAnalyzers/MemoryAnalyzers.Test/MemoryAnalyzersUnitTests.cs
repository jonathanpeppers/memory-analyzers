using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = MemoryAnalyzers.Test.CSharpCodeFixVerifier<
	MemoryAnalyzers.MemoryAnalyzersAnalyzer,
	MemoryAnalyzers.MemoryAnalyzersCodeFixProvider>;

namespace MemoryAnalyzers.Test
{
	[TestClass]
	public class MemoryAnalyzersUnitTest
	{
		//No diagnostics expected to show up
		[TestMethod]
		public async Task TestMethod1()
		{
			var test = @"";

			await VerifyCS.VerifyAnalyzerAsync(test);
		}

		//Diagnostic and CodeFix both triggered and checked for
		[TestMethod]
		public async Task TestMethod2()
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

			var expected = VerifyCS.Diagnostic("MemoryAnalyzers").WithLocation(0).WithArguments("TypeName");
			await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
		}

		[TestMethod]
		public async Task EventHandler()
		{
			var test = """
                using System;

                namespace ConsoleApplication1
                {
                    class Foo
                    {   
                        public event EventHandler {|#0:EventName|};
                    }
                }
            """;

            var expected = VerifyCS.Diagnostic("MemoryAnalyzers").WithLocation(0).WithArguments("EventName");
			await VerifyCS.VerifyAnalyzerAsync(test, expected);
		}
	}
}
