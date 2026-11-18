using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FactoryMES.Contracts;

public static class CollectionFormat
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static DateTime UtcNow()
    {
        var now = DateTime.UtcNow;
        return now.AddTicks(-(now.Ticks % 10));
    }

    public static string Canonical(object value)
    {
        var element = JsonSerializer.SerializeToElement(value, Json);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) Write(writer, element);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void Write(Utf8JsonWriter writer, JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            writer.WriteStartObject();
            foreach (var property in value.EnumerateObject().OrderBy(x => x.Name, StringComparer
                .Ordinal))
            {
                writer.WritePropertyName(property.Name);
                Write(writer, property.Value);
            }
            writer.WriteEndObject();
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            writer.WriteStartArray();
            foreach (var item in value.EnumerateArray()) Write(writer, item);
            writer.WriteEndArray();
        }
        else value.WriteTo(writer);
    }

    public static string Hash(object value) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(Canonical(value)))
    ).ToLowerInvariant();

    public static bool MockEndpoint(string? endpoint) => Uri.TryCreate(
        endpoint,
        UriKind.Absolute,
        out var uri
    ) && uri.Scheme == "http" && uri.Host == "equipment-simulator"
        && uri.Port == 8080 && uri.UserInfo.Length == 0 && uri.Query.Length == 0
        && uri.Fragment.Length == 0
        && Regex.IsMatch(uri.AbsolutePath, "^/equipment/[A-Za-z0-9_.-]{1,40}$")
        && !endpoint!.Contains('%');

    public static bool TryDecimal(string? input, out decimal number)
    {
        number = 0;
        return input is not null && Regex.IsMatch(input,
            @"^-?(?:0|[1-9][0-9]{0,11})(?:\.[0-9]{1,6})?$")
            && decimal.TryParse(
                input,
                NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out number
            );
    }

    public static string Number(decimal value) => value.ToString("0.######", CultureInfo
        .InvariantCulture);
}

