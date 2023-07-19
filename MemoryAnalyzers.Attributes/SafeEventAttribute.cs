namespace MemoryAnalyzers.Attributes;

[AttributeUsage(AttributeTargets.Event)]
public sealed class SafeEventAttribute : Attribute
{
	public SafeEventAttribute(string justification)
	{
		Justification = justification;
	}

	public string Justification { get; private set; }
}
