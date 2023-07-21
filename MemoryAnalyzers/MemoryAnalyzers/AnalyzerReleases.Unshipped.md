; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
MA0001 | Memory | Warning | C# events can cause memory leaks in an NSObject subclass. Add the [MemoryLeakSafe] attribute with a justification as to why the event will not leak.
MA0002 | Memory | Warning | Reference type fields can cause memory leaks in an NSObject subclass. Add the [MemoryLeakSafe] attribute with a justification as to why the field will not leak.