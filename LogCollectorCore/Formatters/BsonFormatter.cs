namespace LogCollectorCore.Formatters
{
	public class BsonFormatter : ILogFormatter
	{
		Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer();

		public string SupportMediaType => "application/bson";

		public LogDynamicObject Convert(byte[] content)
		{
			using (var mem = new System.IO.MemoryStream())
			using (var writer = new Newtonsoft.Json.Bson.BsonDataWriter(mem))
			{
				content = mem.ToArray();
			}

			LogDynamicObject obj = null;

			using (var mem = new System.IO.MemoryStream(content))
			using (var reader = new Newtonsoft.Json.Bson.BsonDataReader(mem))
			{
				obj = serializer.Deserialize<LogDynamicObject>(reader);
			}

			return obj;
		}
	}
}
