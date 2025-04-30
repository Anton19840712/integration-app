using System.Text.Json;

namespace messaging.formatting
{
	public interface IMessageFormatter
	{
		string DecodeUnicodeEscape(string input);
		string FormatJson(string json);
		void WriteFormattedJson(JsonElement element, Utf8JsonWriter writer);
	}
}
