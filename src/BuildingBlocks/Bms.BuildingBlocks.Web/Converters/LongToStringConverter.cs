using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bms.BuildingBlocks.Web.Converters;

/// <summary>
/// Long 类型 JSON 转换器
/// - 序列化时将 long 转为字符串（避免 JavaScript 大整数精度丢失）
/// - 反序列化时同时支持字符串和数字
/// </summary>
public class LongToStringConverter : JsonConverter<long>
{
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (long.TryParse(stringValue, out long result))
            {
                return result;
            }
            throw new JsonException($"无法将字符串 '{stringValue}' 转换为 long 类型");
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetInt64();
        }

        throw new JsonException($"无法将 {reader.TokenType} 转换为 long 类型");
    }

    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

/// <summary>
/// NullableLong 类型 JSON 转换器
/// </summary>
public class NullableLongToStringConverter : JsonConverter<long?>
{
    public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (string.IsNullOrEmpty(stringValue))
            {
                return null;
            }
            if (long.TryParse(stringValue, out long result))
            {
                return result;
            }
            throw new JsonException($"无法将字符串 '{stringValue}' 转换为 long 类型");
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetInt64();
        }

        throw new JsonException($"无法将 {reader.TokenType} 转换为 long? 类型");
    }

    public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString());
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

/// <summary>
/// Int 类型 JSON 转换器（处理前端可能发送的字符串数字）
/// </summary>
public class IntToStringConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (int.TryParse(stringValue, out int result))
            {
                return result;
            }
            throw new JsonException($"无法将字符串 '{stringValue}' 转换为 int 类型");
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetInt32();
        }

        throw new JsonException($"无法将 {reader.TokenType} 转换为 int 类型");
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}
