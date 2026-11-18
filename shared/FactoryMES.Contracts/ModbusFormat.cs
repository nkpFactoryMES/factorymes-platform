using System.Globalization;

namespace FactoryMES.Contracts;

public sealed record ModbusTag(
    ushort Address,
    ushort Width,
    string ValueType,
    string WordOrder
);

// 관리자·Backend·Connector가 같은 주소 규격을 검증한다. 외부 임의 호스트 접근은 허용하지 않는다.
public static class ModbusFormat
{
    public const string Protocol = "MODBUS_TCP";
    public const string Connector = "equipment-modbus-tcp";

    public static bool TryEndpoint(
        string? value,
        out Uri endpoint,
        out byte unitId
    )
    {
        endpoint = null!;
        unitId = 0;
        if (!Uri.TryCreate(
            value,
            UriKind.Absolute,
            out var uri
        )
            || uri.Scheme != "modbus.tcp" || uri.Host != "modbus-simulator"
            || uri.Port != 1502 || uri.UserInfo.Length != 0 || uri.Query.Length != 0
            || uri.Fragment.Length != 0 || value!.Contains('%'))
        {
            return false;
        }
        var parts = uri.AbsolutePath.Split('/');
        if (parts.Length != 3 || parts[1] != "unit"
            || !byte.TryParse(
                parts[2],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out unitId
            )
            || unitId is < 1 or > 247 || parts[2] != unitId.ToString(CultureInfo.InvariantCulture))
        {
            return false;
        }
        endpoint = uri;
        return value == $"modbus.tcp://modbus-simulator:1502/unit/{unitId}";
    }

    // 40001 표기를 암묵 변환하지 않는다. holding:0은 FC03의 wire address 0이다.
    public static bool TryTag(string? value, out ModbusTag tag)
    {
        tag = null!;
        var parts = value?.Split(':');
        if (parts is not { Length: 4 } || parts[0] != "holding"
            || !ushort.TryParse(
                parts[1],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var address
            )
            || parts[1] != address.ToString(CultureInfo.InvariantCulture))
        {
            return false;
        }
        var width = parts[2] is "UINT16" or "INT16" ? 1
            : parts[2] is "UINT32" or "INT32" or "FLOAT32" ? 2 : 0;
        if (width == 0 || address + width > 65536
            || (width == 1 && parts[3] is not ("AB" or "BA"))
            || (width == 2 && parts[3] is not ("ABCD" or "CDAB" or "BADC" or "DCBA")))
        {
            return false;
        }
        tag = new ModbusTag(
            address,
            (ushort)width,
            parts[2],
            parts[3]
        );
        return true;
    }

    public static bool ValidMapping(MappingInput mapping) =>
        TryTag(mapping.SourcePath, out var tag)
        && mapping.DataType == (tag.ValueType == "FLOAT32" ? "DECIMAL" : "INTEGER")
        && (mapping.ValueKind == "NUMBER"
            || (mapping.ValueKind == "COUNTER" && (tag.ValueType is "UINT16" or "UINT32")
                && CollectionFormat.TryDecimal(mapping.Scale, out var scale) && scale == 1
                && CollectionFormat.TryDecimal(mapping.Offset, out var offset) && offset == 0));

    public static bool ValidSpan(IEnumerable<MappingInput> mappings)
    {
        var tags = mappings.Where(x => x.Enabled)
            .Select(x => TryTag(x.SourcePath, out var tag) ? tag : null).ToArray();
        return tags.Length > 0 && tags.All(x => x is not null)
            && tags.Max(x => x!.Address + x.Width) - tags.Min(x => x!.Address) <= 125;
    }
}


