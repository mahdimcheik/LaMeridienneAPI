using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LaMeridienneAPI.Infrastructure;

/// <summary>Instants rendus en UTC avec <c>Z</c> (architecture : fuseau horaire), quel que soit leur décalage.</summary>
public sealed class UtcDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetDateTimeOffset();

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'", CultureInfo.InvariantCulture));
}
