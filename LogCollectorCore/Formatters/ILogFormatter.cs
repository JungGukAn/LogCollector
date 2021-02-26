namespace LogCollectorCore.Formatters
{
	public interface ILogFormatter
	{
		string SupportMediaType { get; }

		LogDynamicObject Convert(byte[] content);
	}
}
