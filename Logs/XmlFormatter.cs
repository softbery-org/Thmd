// Version: 0.1.3.78
using System.IO;
using System.Xml.Serialization;

namespace Thmd.Logs;

public class XmlFormatter : ILogFormatter
{
	public string Format(LogEntry entry)
	{
		XmlSerializer serializer = new XmlSerializer(typeof(LogEntry));
		using StringWriter writer = new StringWriter();
		serializer.Serialize(writer, entry);
		return writer.ToString();
	}
}
