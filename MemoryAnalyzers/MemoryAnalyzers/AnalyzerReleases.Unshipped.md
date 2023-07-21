; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
MA0001 | Memory | Warning | C# events can cause memory leaks in an NSObject subclass. Remove the event or add the [MemoryLeakSafe] attribute with a justification as to why the event will not leak.
MA0002 | Memory | Warning | Reference type members can cause memory leaks in an NSObject subclass. Remove the member, store the value as a WeakReference, or add the [MemoryLeakSafe] attribute with a justification as to why the member will not leak.
MA0003 | Memory | Warning | Subscribing to events with instance methods can cause memory leaks in an NSObject subclass. Remove the subscription or convert the method to a static method.