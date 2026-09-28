namespace HfcDesigner.Core.Formats;

/// <summary>
/// The 512-byte (0x200) header shared by every Lode Data Design Assistant file
/// we've been able to inspect (.NTW, .PAR, .ATV, .TAP, .CPR, .CBL): a
/// null-terminated title string at offset 0 (e.g. "Lode Data Cables File"),
/// an optional "Design x.xx" version tag somewhere in the first 0x80 bytes,
/// a single reserved/unused byte at offset 0x80 followed by a
/// null-terminated facility/system code starting at 0x81, and another
/// reserved byte at 0x90 followed by a null-terminated username/initials
/// field (the last person to save the file) starting at 0x91. Confirmed
/// against real files: byte 0x80 and byte 0x90 are always 0x00, with the
/// text starting one byte later in every sample inspected. Everything
/// after offset 0x200 is format-specific record data.
/// </summary>
public sealed class LodeDataHeader
{
    public const int HeaderLength = 0x200;

    private const int CodeOffset = 0x81;
    private const int UserNameOffset = 0x91;

    public required string Title { get; init; }
    public string? DesignVersion { get; init; }
    public string Code { get; init; } = "";
    public string UserName { get; init; } = "";

    public static LodeDataHeader Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < HeaderLength)
        {
            throw new InvalidDataException(
                $"File is only {data.Length} bytes long; a Lode Data file header requires at least {HeaderLength} bytes.");
        }

        var title = ReadAsciiZ(data, 0, CodeOffset);
        var code = ReadAsciiZ(data, CodeOffset, UserNameOffset - CodeOffset);
        var userName = ReadAsciiZ(data, UserNameOffset, HeaderLength - UserNameOffset);
        var version = FindDesignVersion(data[..CodeOffset]);

        return new LodeDataHeader
        {
            Title = title,
            DesignVersion = version,
            Code = code,
            UserName = userName,
        };
    }

    private static string? FindDesignVersion(ReadOnlySpan<byte> data)
    {
        ReadOnlySpan<byte> marker = "Design "u8;
        var idx = data.IndexOf(marker);
        if (idx < 0) return null;

        var rest = data[idx..];
        return ReadAsciiZ(rest, 0, rest.Length);
    }

    private static string ReadAsciiZ(ReadOnlySpan<byte> data, int offset, int maxLength)
    {
        if (offset >= data.Length) return "";

        var available = Math.Min(maxLength, data.Length - offset);
        var slice = data.Slice(offset, available);

        var terminator = slice.IndexOf((byte)0);
        if (terminator >= 0) slice = slice[..terminator];

        Span<char> chars = stackalloc char[slice.Length];
        var count = 0;
        foreach (var b in slice)
        {
            if (b is >= 0x20 and < 0x7f) chars[count++] = (char)b;
        }

        return new string(chars[..count]).Trim();
    }
}
