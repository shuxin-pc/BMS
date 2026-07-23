using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bms.System.Api.Converters;

/// <summary>
/// 将 "2026-07-10" 格式的日期字符串解析为 DateTime（时间为00:00:00）
/// </summary>
public class DateOnlyToUtcConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return default;

        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
            return default;

        // 解析 "2026-07-10" 格式，设置为当天 00:00:00（无时区信息）
        if (DateTime.TryParseExact(value, "yyyy-MM-dd", null, global::System.Globalization.DateTimeStyles.None, out var result))
        {
            return new DateTime(result.Year, result.Month, result.Day, 0, 0, 0, DateTimeKind.Unspecified);
        }

        throw new JsonException($"无法解析日期: {value}");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // 输出为 "yyyy-MM-dd" 格式
        writer.WriteStringValue(value.ToString("yyyy-MM-dd"));
    }
}

/// <summary>
/// 可空的日期转换器
/// </summary>
public class NullableDateOnlyToUtcConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (string.IsNullOrEmpty(value))
                return null;

            // 解析 "2026-07-10" 格式，设置为当天 00:00:00（无时区信息）
            if (DateTime.TryParseExact(value, "yyyy-MM-dd", null, global::System.Globalization.DateTimeStyles.None, out var result))
            {
                return new DateTime(result.Year, result.Month, result.Day, 0, 0, 0, DateTimeKind.Unspecified);
            }
        }

        throw new JsonException($"无法解析日期: {reader.GetString()}");
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd"));
        }
    }
}
