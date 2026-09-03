using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VisH.Model.GeneralUtils;

public class TimeSpanJsonConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return TimeSpan.Zero;
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            return TimeSpan.FromSeconds(reader.GetDouble());
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected string or number for TimeSpan, got {reader.TokenType}.");
        }

        string? value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            return TimeSpan.Zero;
        }

        // 1. Try standard TimeSpan parsing
        if (TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var timeSpan))
        {
            return timeSpan;
        }

        // 2. Handle extended hours format: "HH:mm:ss" or "HH:mm:ss.fff" (where HH >= 24)
        var parts = value.Split(':');
        if (parts.Length == 3)
        {
            if (int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int hours) &&
                int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int minutes) &&
                double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds))
            {
                return TimeSpan.FromHours(hours)
                    .Add(TimeSpan.FromMinutes(minutes))
                    .Add(TimeSpan.FromSeconds(seconds));
            }
        }
        else if (parts.Length == 4)
        {
            // Handle "days:hours:minutes:seconds" format
            if (int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int days) &&
                int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int hours) &&
                int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int minutes) &&
                double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds))
            {
                return new TimeSpan(days, hours, minutes, (int)seconds, (int)((seconds - (int)seconds) * 1000));
            }
        }

        return TimeSpan.Zero;
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("c", CultureInfo.InvariantCulture));
    }
}