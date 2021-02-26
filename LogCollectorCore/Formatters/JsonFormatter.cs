using System.Text;
using Newtonsoft.Json;

namespace LogCollectorCore.Formatters
{
	public class JsonFormatter : ILogFormatter
	{
		public string SupportMediaType => "application/json";

		public LogDynamicObject Convert(byte[] content)
		{
			var jsonString = Encoding.UTF8.GetString(content);
			var jsonObject = JsonConvert.DeserializeObject<LogDynamicObject>(jsonString);

			return jsonObject;
		}
	}
}
