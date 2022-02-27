using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DotNetReleaser.Helpers;

public class JsonHelper
{
    private static readonly JsonDocumentOptions CommonOptions = new JsonDocumentOptions()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    public static object? FromFile(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return FromStream(stream);
    }

    public static object? FromStream(Stream stream)
    {
        var jsonDoc = JsonDocument.Parse(stream, CommonOptions);
        return ConvertFromJson(jsonDoc.RootElement);
    }

    public static object? FromString(string json)
    {
        var jsonDoc = JsonDocument.Parse(json, CommonOptions);
        return ConvertFromJson(jsonDoc.RootElement);
    }

    private static object? ConvertFromJson(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var obj = new Dictionary<string, object?>();
                foreach (var prop in element.EnumerateObject())
                {
                    obj[prop.Name] = ConvertFromJson(prop.Value);
                }

                return obj;
            case JsonValueKind.Array:
                var array = new List<object?>();
                foreach (var nestedElement in element.EnumerateArray())
                {
                    array.Add(ConvertFromJson(nestedElement));
                }
                return array;
            case JsonValueKind.String:
                return element.GetString();
            case JsonValueKind.Number:
                if (element.TryGetInt32(out var intValue))
                {
                    return intValue;
                }
                else if (element.TryGetInt64(out var longValue))
                {
                    return longValue;
                }
                else if (element.TryGetUInt32(out var uintValue))
                {
                    return uintValue;
                }
                else if (element.TryGetUInt64(out var ulongValue))
                {
                    return ulongValue;
                }
                else if (element.TryGetDecimal(out var decimalValue))
                {
                    return decimalValue;
                }
                else if (element.TryGetDouble(out var doubleValue))
                {
                    return doubleValue;
                }
                else
                {
                    throw new InvalidOperationException($"Unable to convert number {element}");
                }
            case JsonValueKind.True:
                return BoolTrue;
            case JsonValueKind.False:
                return BoolFalse;
            default:
                return null;
        }
    }
    private static readonly object BoolTrue = true;
    private static readonly object BoolFalse = false;

}