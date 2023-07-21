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

		[TestMethod]
		public async Task NotNSObject()
		{
			var test = """
				class Foo
				{
					public event EventHandler {|#0:EventName|};
				}
			""";

			await VerifyCS.VerifyAnalyzerAsync(test);
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
		public async Task Event()
		{
			var test = """
				class Foo : NSObject
				{
					public event EventHandler {|#0:EventName|};
				}
			""";

			var expected = VerifyCS.Diagnostic("MA0001").WithLocation(0).WithArguments("EventName");
			await VerifyCS.VerifyAnalyzerAsync(test, expected);
		}

		[TestMethod]
		public async Task SafeEvent()
		{
			var test = """
				class Foo : NSObject
				{
					[MemoryLeakSafe("Event tested via MemoryTests.MyEvent")]
					public event EventHandler {|#0:EventName|};
				}
			""";

			// 0 warnings
			await VerifyCS.VerifyAnalyzerAsync(test);
		}

		[TestMethod]
		public async Task EventThatIsOk()
		{
			var test = """
				class Foo : NSObject
				{
					event EventHandler {|#0:EventName|};
				}
			""";

			// 0 warnings
			await VerifyCS.VerifyAnalyzerAsync(test);
		}

		[TestMethod]
		public async Task EventNamedArguments()
		{
			var test = """
				[Register(Name = "UITextField", IsWrapper = true)]
				class UITextField { }

				class Foo : UITextField
				{
					public event EventHandler {|#0:EventName|};
				}
			""";

			var expected = VerifyCS.Diagnostic("MA0001").WithLocation(0).WithArguments("EventName");
			await VerifyCS.VerifyAnalyzerAsync(test, expected);
		}

		[TestMethod]
		public async Task MyView()
		{
			var test = """
				class MyView : UIView { }

				class Foo : MyView
				{
					public event EventHandler {|#0:EventName|};
				}
			""";

			var expected = VerifyCS.Diagnostic("MA0001").WithLocation(0).WithArguments("EventName");
			await VerifyCS.VerifyAnalyzerAsync(test, expected);
		}

		[TestMethod]
		public async Task FieldThatLeaks()
		{
			var test = """
				class MyViewSubclass : UIView
				{
					public UIView {|#0:FieldName|};
				}
			""";

			var expected = VerifyCS.Diagnostic("MA0002").WithLocation(0).WithArguments("FieldName");
			await VerifyCS.VerifyAnalyzerAsync(test, expected);
		}

		[TestMethod]
		public async Task FieldsThatAreOK()
		{
			var test = """
				class MyViewSubclass : UIView
				{
					public WeakReference Foo;
					public WeakReference<UIView> Bar;
					public double Baz;
				}
			""";

			await VerifyCS.VerifyAnalyzerAsync(test);
		}

		[TestMethod]
		public async Task SafeField()
		{
			var test = """
				class MyViewSubclass : UIView
				{
					[MemoryLeakSafe("Field tested via MemoryTests.MyField")]
					public UIView {|#0:FieldName|};
				}
			""";

			await VerifyCS.VerifyAnalyzerAsync(test);
		}

		[TestMethod]
		public async Task PropertyThatLeaks()
		{
			var test = """
				class MyViewSubclass : UIView
				{
					public UIView {|#0:FieldName|} { get; set; }
				}
			""";

			var expected = VerifyCS.Diagnostic("MA0002").WithLocation(0).WithArguments("FieldName");
			await VerifyCS.VerifyAnalyzerAsync(test, expected);
		}

		[TestMethod]
		public async Task PropertiesThatAreOK()
		{
			var test = """
				class MyViewSubclass : UIView
				{
					public WeakReference Foo { get; set; }
					public WeakReference<UIView> Bar { get; set; }
					public double Baz { get; set; }
					public UIView EmptyProperty
					{
						set => throw new NotImplementedException();
						get => throw new NotImplementedException();
					}
				}
			""";

			await VerifyCS.VerifyAnalyzerAsync(test);
		}

		[TestMethod]
		public async Task SafeProperty()
		{
			var test = """
				class MyViewSubclass : UIView
				{
					[MemoryLeakSafe("Property tested via MemoryTests.MyProperty")]
					public UIView {|#0:FieldName|} { get; set; }
				}
			""";

			await VerifyCS.VerifyAnalyzerAsync(test);
		}

		[TestMethod]
		public async Task SubscriptionThatLeaks()
		{
			var test = """
				[Register(Name = "UITextField", IsWrapper = true)]
				class UITextField
				{
					[MemoryLeakSafe("Ignore for this test")]
					public event EventHandler EditingDidBegin;
				}

				class MyView : UIView
				{
					public MyView()
					{
						new UITextField().EditingDidBegin += {|#0:OnEditingDidBegin|};
					}

					void OnEditingDidBegin(object sender, EventArgs e) { }
				}
			""";

			var expected = VerifyCS.Diagnostic("MA0003").WithLocation(0).WithArguments("OnEditingDidBegin");
			await VerifyCS.VerifyAnalyzerAsync(test, expected);
		}

		[TestMethod]
		public async Task SubscriptionLocalThatLeaks()
		{
			var test = """
				[Register(Name = "UITextField", IsWrapper = true)]
				class UITextField
				{
					[MemoryLeakSafe("Ignore for this test")]
					public event EventHandler EditingDidBegin;
				}

				class MyView : UIView
				{
					public MyView()
					{
						var field = new UITextField();
						field.EditingDidBegin += {|#0:OnEditingDidBegin|};
					}

					void OnEditingDidBegin(object sender, EventArgs e) { }
				}
			""";

			var expected = VerifyCS.Diagnostic("MA0003").WithLocation(0).WithArguments("OnEditingDidBegin");
			await VerifyCS.VerifyAnalyzerAsync(test, expected);
		}

		[TestMethod]
		public async Task SubscriptionThatIsOK()
		{
			var test = """
				[Register(Name = "UITextField", IsWrapper = true)]
				class UITextField
				{
					[MemoryLeakSafe("Ignore for this test")]
					public event EventHandler EditingDidBegin;
				}

				class MySubclass : UIView
				{
					[MemoryLeakSafe("Ignore for this test")]
					public event EventHandler InheritedEvent;
				}

				class MyView : MySubclass
				{
					[MemoryLeakSafe("Ignore for this test")]
					public event EventHandler MyOwnedEvent;

					event EventHandler MyPrivateEvent;

					public MyView()
					{
						MyOwnedEvent += OnMyOwnedEvent;
						this.MyOwnedEvent += OnMyOwnedEvent;
						InheritedEvent += OnInheritedEvent;
						this.InheritedEvent += OnInheritedEvent;

						new UITextField().EditingDidBegin += OnEditingDidBegin;
					}

					void OnMyOwnedEvent(object sender, EventArgs e) { }

					void OnInheritedEvent(object sender, EventArgs e) { }

					static void OnEditingDidBegin(object sender, EventArgs e) { }
				}
			""";

			await VerifyCS.VerifyAnalyzerAsync(test);
		}

		[TestMethod]
		public async Task SubscriptionMauiDatePicker()
		{
			var test = """
				[Register("UIControl", true)]
				class UIControl
				{
					[MemoryLeakSafe("Ignore for this test")]
					public event EventHandler ValueChanged;
				}

				[Register("UITextField", true)]
				class UITextField : UIControl
				{
				}

				[Register("UIDatePicker", true)]
				class UIDatePicker : UIControl
				{
				}

				class NoCaretField : UITextField
				{
				}

				class MauiDatePicker : NoCaretField
				{
					public MauiDatePicker()
					{
						var picker = new UIDatePicker();
						picker.ValueChanged += {|#0:OnValueChanged|};
					}

					void OnValueChanged(object sender, EventArgs e) { }
				}
			""";

			var expected = VerifyCS.Diagnostic("MA0003").WithLocation(0).WithArguments("OnValueChanged");
			await VerifyCS.VerifyAnalyzerAsync(test, expected);
		}
	}
}
