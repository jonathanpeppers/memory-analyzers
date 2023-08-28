# memory-analyzers

 [![NuGet](https://buildstats.info/nuget/MemoryAnalyzers?includePreReleases=true)](https://www.nuget.org/packages/MemoryAnalyzers/)

A set of Roslyn C# code analyzers for finding memory leaks in iOS and
MacCatalyst applications -- with potentially other ideas in the future.

For more information on the "circular reference" problem on Apple platforms,
see:

* https://github.com/dotnet/maui/wiki/Memory-Leaks#circular-references-on-ios-and-catalyst

Note that this "circular reference" situation would occur in Swift or
Objective-C, so it is not a .NET-specific problem. It does not occur on Android
or Windows platforms.

## MA0001

Don't define `public` events in `NSObject` subclasses:

```csharp
public class MyView : UIView
{
    // NOPE!
    public event EventHandler MyEvent;
}
```

![Example of MA0001](docs/images/MA0001.png)

## MA0002

Don't declare members in `NSObject` subclasses unless they are:

* `WeakReference` or `WeakReference<T>`
* Value types

```csharp
class MyView : UIView
{
    // NOPE!
    public UIView? Parent { get; set; }

    public void Add(MyView subview)
    {
        subview.Parent = this;
        AddSubview(subview);
    }
}
```

![Example of MA0002](docs/images/MA0002.png)

## MA0003

Don't subscribe to events inside `NSObject` subclasses unless:

* It's your event via `this.MyEvent` or from a base type.
* The method is `static`.

```csharp
class MyView : UIView
{
    public MyView()
    {
        var picker = new UIDatePicker();
        AddSubview(picker);
        picker.ValueChanged += OnValueChanged;
    }
    
    void OnValueChanged(object sender, EventArgs e) { }

    // Use this instead and it doesn't leak!
    //static void OnValueChanged(object sender, EventArgs e) { }
}
```

![Example of MA0003](docs/images/MA0003.png)
