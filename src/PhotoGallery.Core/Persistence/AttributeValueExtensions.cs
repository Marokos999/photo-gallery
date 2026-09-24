using System.Globalization;
using Amazon.DynamoDBv2.Model;

namespace PhotoGallery.Core.Persistence;

internal static class AttributeValueExtensions
{
    public static AttributeValue ToS(this string value) => new() { S = value };

    public static AttributeValue ToN(this int value) => new() { N = value.ToString(CultureInfo.InvariantCulture) };

    public static AttributeValue ToS(this DateTimeOffset value) =>
        new() { S = value.ToString("O", CultureInfo.InvariantCulture) };

    public static string GetString(this Dictionary<string, AttributeValue> item, string key) =>
        item.GetStringOrNull(key) ?? throw new InvalidOperationException($"Missing attribute '{key}'.");

    public static string? GetStringOrNull(this Dictionary<string, AttributeValue> item, string key) =>
        item.TryGetValue(key, out var value) ? value.S : null;

    public static int GetInt(this Dictionary<string, AttributeValue> item, string key) =>
        item.GetIntOrNull(key) ?? throw new InvalidOperationException($"Missing attribute '{key}'.");

    public static int? GetIntOrNull(this Dictionary<string, AttributeValue> item, string key) =>
        item.TryGetValue(key, out var value) && value.N is { } n
            ? int.Parse(n, CultureInfo.InvariantCulture)
            : null;

    public static DateTimeOffset GetDate(this Dictionary<string, AttributeValue> item, string key) =>
        DateTimeOffset.Parse(item.GetString(key), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    public static List<string> GetStringList(this Dictionary<string, AttributeValue> item, string key) =>
        item.TryGetValue(key, out var value) && value.L is { } list
            ? list.Select(v => v.S).ToList()
            : [];
}