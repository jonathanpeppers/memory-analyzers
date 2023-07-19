; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
MA0001 | Memory | Warning | C# events can cause memory leaks. Add the `[SafeEvent]` attribute with a justification as to why the event will not leak.