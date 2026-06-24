using System.Text.Json;
using System.Text.Json.Serialization;

namespace TaskManagement.API.Converters;

public class CustomDateTimeConverter(string format) : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => DateTime.Parse(reader.GetString()!);

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString(format));
}
