namespace MemoryAnalyzers.Attributes;

[AttributeUsage(AttributeTargets.Event | AttributeTargets.Field | AttributeTargets.Property)]
public sealed class MemoryLeakSafeAttribute : Attribute
{
	public MemoryLeakSafeAttribute(string justification)
	{
		Justification = justification;
	}

	public string Justification { get; private set; }
}
