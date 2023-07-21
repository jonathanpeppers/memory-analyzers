# memory-analyzers

A set of Roslyn C# code analyzers for finding memory leaks

This is still WIP, but the idea is:

![Screenshot of Memory Analyzer](docs/images/analyzer.jpg)

Which you'd solve by adding:

```csharp
[MemoryLeakSafe("This event is internal, runs only on Windows, the subscriber has the same lifetime as the publisher.")]
```

Or:

```csharp
[MemoryLeakSafe("This event is proven safe by the FooTests.Bar test.")]
```
