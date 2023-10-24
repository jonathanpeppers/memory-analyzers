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
		public async Task MEM0001_Remove()
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

			var expected = VerifyCS.Diagnostic("MEM0001").WithLocation(0).WithArguments("EventName");
			await VerifyCS.VerifyCodeFixAsync(test, expected, codefix, index: 0);
		}

		[TestMethod]
		public async Task MEM0001_UnconditionalSuppressMessage()
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
			    [UnconditionalSuppressMessage("Memory", "MEM0001", Justification = "Proven safe in test: XYZ")]
			    public event EventHandler {|#0:EventName|};
			}
			""";

			var expected = VerifyCS.Diagnostic("MEM0001").WithLocation(0).WithArguments("EventName");
			await VerifyCS.VerifyCodeFixAsync(test, expected, codefix, index: 1);
		}

		[TestMethod]
		public async Task MEM0002_Remove()
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

			var expected = VerifyCS.Diagnostic("MEM0002").WithLocation(0).WithArguments("FieldName");
			await VerifyCS.VerifyCodeFixAsync(test, expected, codefix, index: 0);
		}

		[TestMethod]
		public async Task MEM0002_UnconditionalSuppressMessage()
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
			    [UnconditionalSuppressMessage("Memory", "MEM0002", Justification = "Proven safe in test: XYZ")]
			    public UIView {|#0:FieldName|};
			}
			""";

			var expected = VerifyCS.Diagnostic("MEM0002").WithLocation(0).WithArguments("FieldName");
			await VerifyCS.VerifyCodeFixAsync(test, expected, codefix, index: 1);
		}

		[TestMethod]
		public async Task MEM0002_MakeWeak_Field()
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
			    public WeakReference<UIView> {|#0:FieldName|};
			}
			""";

			var expected = VerifyCS.Diagnostic("MEM0002").WithLocation(0).WithArguments("FieldName");
			await VerifyCS.VerifyCodeFixAsync(test, expected, codefix, index: 2);
		}

		[TestMethod]
		public async Task MEM0002_MakeWeak_Property()
		{
			var test = """
			class Foo : NSObject
			{
			    public UIView {|#0:FieldName|} { get; set; }
			}
			""";

			var codefix = """
			class Foo : NSObject
			{
			    public WeakReference<UIView> {|#0:FieldName|} { get; set; }
			}
			""";

			var expected = VerifyCS.Diagnostic("MEM0002").WithLocation(0).WithArguments("FieldName");
			await VerifyCS.VerifyCodeFixAsync(test, expected, codefix, index: 2);
		}

		[TestMethod]
		public async Task MEM0003_Remove()
		{
			var test = """
			using System.Diagnostics.CodeAnalysis;

			[Register("UITextField", true)]
			class UITextField
			{
			    [UnconditionalSuppressMessage("Memory", "MEM0001")]
			    public event EventHandler EditingDidBegin;
			}

			class Foo : NSObject
			{
			    public Foo()
			    {
			        new UITextField().EditingDidBegin += {|#0:OnEditingDidBegin|};
			    }

			    void OnEditingDidBegin(object sender, EventArgs e)
			    {
			    }
			}
			""";

			var codefix = """
			using System.Diagnostics.CodeAnalysis;

			[Register("UITextField", true)]
			class UITextField
			{
			    [UnconditionalSuppressMessage("Memory", "MEM0001")]
			    public event EventHandler EditingDidBegin;
			}

			class Foo : NSObject
			{
			    public Foo()
			    {
			        
			    }

			    void OnEditingDidBegin(object sender, EventArgs e)
			    {
			    }
			}
			""";

			var expected = VerifyCS.Diagnostic("MEM0003").WithLocation(0).WithArguments("OnEditingDidBegin");
			await VerifyCS.VerifyCodeFixAsync(test, expected, codefix, index: 0);
		}

		[TestMethod]
		public async Task MEM0003_MakeStatic()
		{
			var test = """
			using System.Diagnostics.CodeAnalysis;

			[Register("UITextField", true)]
			class UITextField
			{
			    [UnconditionalSuppressMessage("Memory", "MEM0001")]
			    public event EventHandler EditingDidBegin;
			}

			class Foo : NSObject
			{
			    public Foo()
			    {
			        new UITextField().EditingDidBegin += {|#0:OnEditingDidBegin|};
			    }

			    void OnEditingDidBegin(object sender, EventArgs e)
			    {
			    }
			}
			""";

			var codefix = """
			using System.Diagnostics.CodeAnalysis;

			[Register("UITextField", true)]
			class UITextField
			{
			    [UnconditionalSuppressMessage("Memory", "MEM0001")]
			    public event EventHandler EditingDidBegin;
			}

			class Foo : NSObject
			{
			    public Foo()
			    {
			        new UITextField().EditingDidBegin += {|#0:OnEditingDidBegin|};
			    }

			    static void OnEditingDidBegin(object sender, EventArgs e)
			    {
			    }
			}
			""";

			var expected = VerifyCS.Diagnostic("MEM0003").WithLocation(0).WithArguments("OnEditingDidBegin");
			await VerifyCS.VerifyCodeFixAsync(test, expected, codefix, index: 1);
		}
	}
}
