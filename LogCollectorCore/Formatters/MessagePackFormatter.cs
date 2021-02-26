using MessagePack;

namespace LogCollectorCore.Formatters
{
	public class MessagePackFormatter : ILogFormatter
	{
		public string SupportMediaType => "application/msgpack";

		public LogDynamicObject Convert(byte[] content)
		{
			return MessagePackSerializer.Deserialize<LogDynamicObject>(content);
		}
	}
}
