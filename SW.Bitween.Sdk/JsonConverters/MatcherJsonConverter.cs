using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SW.Bitween.Model;

namespace SW.Bitween.JsonConverters;

public class MatcherJsonConverter : JsonConverter<Matcher>
{
    public override void WriteJson(JsonWriter writer, Matcher? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartObject();

        switch (value)
        {
            case ContainsMatcher contains:
                writer.WritePropertyName("type");
                writer.WriteValue("contains");
                writer.WritePropertyName("value");
                writer.WriteValue(contains.Value);
                writer.WritePropertyName("caseSensitive");
                writer.WriteValue(contains.CaseSensitive);
                break;

            case RegexMatcher regex:
                writer.WritePropertyName("type");
                writer.WriteValue("regex");
                writer.WritePropertyName("pattern");
                writer.WriteValue(regex.Pattern);
                writer.WritePropertyName("flags");
                writer.WriteValue(regex.Flags);
                break;

            case ExceptionTypeMatcher exceptionType:
                writer.WritePropertyName("type");
                writer.WriteValue("exceptionType");
                writer.WritePropertyName("value");
                writer.WriteValue(exceptionType.Value);
                writer.WritePropertyName("includeInner");
                writer.WriteValue(exceptionType.IncludeInner);
                break;

            case JsonPathMatcher jsonPath:
                writer.WritePropertyName("type");
                writer.WriteValue("jsonPath");
                writer.WritePropertyName("path");
                writer.WriteValue(jsonPath.Path);
                writer.WritePropertyName("op");
                writer.WriteValue(jsonPath.Op.ToString());
                writer.WritePropertyName("value");
                writer.WriteValue(jsonPath.Value);
                break;

            default:
                throw new JsonSerializationException($"Unknown Matcher type '{value.GetType()}'");
        }

        writer.WriteEndObject();
    }

    public override Matcher? ReadJson(JsonReader reader, Type objectType, Matcher? existingValue,
        bool hasExistingValue, JsonSerializer serializer)
    {
        var jObject = serializer.Deserialize<JObject>(reader);
        if (jObject is null) return null;

        var type = jObject.Property("type")?.Value?.ToString();
        switch (type)
        {
            case "contains":
                return new ContainsMatcher
                {
                    Value = Required(jObject, "value", "contains"),
                    CaseSensitive = jObject.Property("caseSensitive")?.Value?.ToObject<bool>() ?? false
                };

            case "regex":
                return new RegexMatcher
                {
                    Pattern = Required(jObject, "pattern", "regex"),
                    Flags = jObject.Property("flags")?.Value?.ToString() ?? "i"
                };

            case "exceptionType":
                return new ExceptionTypeMatcher
                {
                    Value = Required(jObject, "value", "exceptionType"),
                    IncludeInner = jObject.Property("includeInner")?.Value?.ToObject<bool>() ?? true
                };

            case "jsonPath":
                return new JsonPathMatcher
                {
                    Path = Required(jObject, "path", "jsonPath"),
                    Op = Enum.Parse<JsonPathOp>(jObject.Property("op")?.Value?.ToString() ?? nameof(JsonPathOp.Eq)),
                    Value = jObject.Property("value")?.Value?.ToString()
                };

            default:
                throw new JsonSerializationException($"Unknown or missing Matcher discriminator 'type': '{type}'");
        }
    }

    /// <summary>
    /// A matcher missing the field it matches on cannot match anything, and silently building one
    /// defers the failure to retry-evaluation time as a NullReferenceException with no clue in it.
    /// Failing here names the field and the matcher type.
    /// </summary>
    private static string Required(JObject jObject, string field, string matcherType) =>
        jObject.Property(field)?.Value?.ToString()
        ?? throw new JsonSerializationException(
            $"Matcher of type '{matcherType}' is missing its required '{field}'.");
}
