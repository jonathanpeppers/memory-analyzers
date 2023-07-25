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
		public async Task UnconditionalSuppressMessage_MA0001()
		{
			var test = """
				class Foo : NSObject
				{
					[UnconditionalSuppressMessage("Memory", "MA0001")]
					public event EventHandler {|#0:EventName|};
				}
			""";

			// 0 warnings
			await VerifyCS.VerifyAnalyzerAsync(test);
		}

		[TestMethod]
		public async Task UnconditionalSuppressMessage_MA0001_Justification()
		{
			var test = """
				class Foo : NSObject
				{
					[UnconditionalSuppressMessage("Memory", "MA0001", Justification = "I know what I'm doing! LOL?")]
					public event EventHandler {|#0:EventName|};
				}
			""";

			// 0 warnings
			await VerifyCS.VerifyAnalyzerAsync(test);
		}

		[TestMethod]
		public async Task UnconditionalSuppressMessage_WrongCode()
		{
			var test = """
				class Foo : NSObject
				{
					[UnconditionalSuppressMessage("Memory", "ABC1234")]
					public event EventHandler {|#0:EventName|};
				}
			""";

			var expected = VerifyCS.Diagnostic("MA0001").WithLocation(0).WithArguments("EventName");
			await VerifyCS.VerifyAnalyzerAsync(test, expected);
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
		public async Task FieldThatLeaks_Object()
		{
			var test = """
				class MyViewSubclass : UIView
				{
					public object {|#0:FieldName|};
				}
			""";

			var expected = VerifyCS.Diagnostic("MA0002").WithLocation(0).WithArguments("FieldName");
			await VerifyCS.VerifyAnalyzerAsync(test, expected);
		}

		[TestMethod]
		public async Task FieldThatLeaks_Delegate()
		{
			var test = """
				class MyViewSubclass : UIView
				{
					public Action {|#0:FieldName|};
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
					public string Text;
					public CALayer MyLayer;
					public UIColor MyColor;
					public double[] MyArray;
				}
			""";

			await VerifyCS.VerifyAnalyzerAsync(test);
		}

		[TestMethod]
		public async Task UIView_CAShapeLayer()
		{
			var test = """
				namespace UIKit
				{
					[Register("CAShapeLayer", true)]
					class CAShapeLayer : CALayer { }
				}

				class MyViewSubclass : UIView
				{
					public CAShapeLayer MyLayer;
				}
			""";

			await VerifyCS.VerifyAnalyzerAsync(test);
		}

		[TestMethod]
		public async Task UnconditionalSuppressMessage_MA0002_Field()
		{
			var test = """
				class MyViewSubclass : UIView
				{
					[UnconditionalSuppressMessage("Memory", "MA0002")]
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
					public UIView {|#0:PropertyName|} { get; set; }
				}
			""";

			var expected = VerifyCS.Diagnostic("MA0002").WithLocation(0).WithArguments("PropertyName");
			await VerifyCS.VerifyAnalyzerAsync(test, expected);
		}

		[TestMethod]
		public async Task PropertyThatLeaks_Object()
		{
			var test = """
				class MyViewSubclass : UIView
				{
					public object {|#0:PropertyName|} { get; set; }
				}
			""";

			var expected = VerifyCS.Diagnostic("MA0002").WithLocation(0).WithArguments("PropertyName");
			await VerifyCS.VerifyAnalyzerAsync(test, expected);
		}

		[TestMethod]
		public async Task PropertyThatLeaks_Delegate()
		{
			var test = """
				class MyViewSubclass : UIView
				{
					public Action {|#0:PropertyName|} { get; set; }
				}
			""";

			var expected = VerifyCS.Diagnostic("MA0002").WithLocation(0).WithArguments("PropertyName");
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
					public string Text { get; set; }
					public UIView EmptyProperty
					{
						set => throw new NotImplementedException();
						get => throw new NotImplementedException();
					}
					public CALayer MyLayer { get; set; }
					public UIColor MyColor { get; set; }
					public double[] MyArray { get; set; }
				}
			""";

			await VerifyCS.VerifyAnalyzerAsync(test);
		}

		[TestMethod]
		public async Task UIApplicationDelegate_Window()
		{
			var test = """
				namespace UIKit
				{
					class UIApplicationDelegate : NSObject { }

					class UIWindow : NSObject { }
				}

				class MyAppDelegate : UIApplicationDelegate
				{
					public virtual UIWindow Window { get; set; }
				}
			""";

			await VerifyCS.VerifyAnalyzerAsync(test);
		}

		[TestMethod]
		public async Task IUIApplicationDelegate_Window()
		{
			var test = """
				namespace UIKit
				{
					interface IUIApplicationDelegate { }

					class UIWindow : NSObject { }
				}

				class MyAppDelegate : NSObject, IUIApplicationDelegate
				{
					public virtual UIWindow Window { get; set; }
				}
			""";

			await VerifyCS.VerifyAnalyzerAsync(test);
		}

		[TestMethod]
		public async Task UnconditionalSuppressMessage_MA0002_Property()
		{
			var test = """
				class MyViewSubclass : UIView
				{
					[UnconditionalSuppressMessage("Memory", "MA0002")]
					public UIView {|#0:PropertyName|} { get; set; }
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
					[UnconditionalSuppressMessage("Memory", "MA0001")]
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
					[UnconditionalSuppressMessage("Memory", "MA0001")]
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
					[UnconditionalSuppressMessage("Memory", "MA0001")]
					public event EventHandler EditingDidBegin;
				}

				class MySubclass : UIView
				{
					[UnconditionalSuppressMessage("Memory", "MA0001")]
					public event EventHandler InheritedEvent;
				}

				class MyView : MySubclass
				{
					[UnconditionalSuppressMessage("Memory", "MA0001")]
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
					[UnconditionalSuppressMessage("Memory", "MA0001")]
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
