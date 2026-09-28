namespace HfcDesigner.Core.Formats;

/// <summary>
/// Reads Lode Data spec files whose body is a flat array of fixed-length
/// records, each holding a null-terminated ASCII name near its start —
/// confirmed by inspecting real .CBL files (384-byte records, name at
/// relative offset 5) and .ATV files (318-byte records, name at relative
/// offset 0). The record start offset is auto-detected by looking for three
/// consecutive record-length strides that all begin with plausible name
/// text, rather than a hardcoded file-specific offset, so it keeps working
/// if a file's preamble/header data is a different size than our samples.
/// </summary>
public static class FixedRecordScanner
{
    public static IReadOnlyList<SpecFileRecord> Scan(byte[] data, int recordLength, int nameOffsetInRecord = 0)
    {
        var start = DetectFirstRecordOffset(data, recordLength, nameOffsetInRecord);
        if (start is null) return Array.Empty<SpecFileRecord>();

        var records = new List<SpecFileRecord>();
        var index = 0;
        for (var offset = start.Value; offset + recordLength <= data.Length; offset += recordLength)
        {
            var raw = new byte[recordLength];
            Array.Copy(data, offset, raw, 0, recordLength);
            var name = ReadAsciiZName(raw, nameOffsetInRecord);
            records.Add(new SpecFileRecord(index++, offset, name, raw));
        }
        return records;
    }

    private static long? DetectFirstRecordOffset(
        byte[] data, int recordLength, int nameOffsetInRecord, int searchFrom = LodeDataHeader.HeaderLength)
    {
        var limit = data.Length - 3L * recordLength;
        for (var candidate = (long)searchFrom; candidate < limit; candidate++)
        {
            if (LooksLikeNameStart(data, candidate + nameOffsetInRecord) &&
                LooksLikeNameStart(data, candidate + nameOffsetInRecord + recordLength) &&
                LooksLikeNameStart(data, candidate + nameOffsetInRecord + 2L * recordLength))
            {
                return candidate;
            }
        }
        return null;
    }

    /// <summary>
    /// Equipment/cable model names in these files always start with at
    /// least two uppercase letters or digits (e.g. "P3-500JCA-EX",
    /// "BLE120S", "RG-6 AER") — the third character is often a dash or
    /// space, so only the first two bytes are checked.
    /// </summary>
    private static bool LooksLikeNameStart(byte[] data, long offset)
    {
        if (offset < 0 || offset + 2 > data.Length) return false;
        for (var i = 0; i < 2; i++)
        {
            var b = data[offset + i];
            var isNameChar = (b >= (byte)'A' && b <= (byte)'Z') || (b >= (byte)'0' && b <= (byte)'9');
            if (!isNameChar) return false;
        }
        return true;
    }

    private static string ReadAsciiZName(byte[] record, int offset)
    {
        if (offset >= record.Length) return "";

        var end = Array.IndexOf(record, (byte)0, offset);
        if (end < 0) end = record.Length;
        var length = end - offset;

        Span<char> chars = stackalloc char[length];
        var count = 0;
        for (var i = 0; i < length; i++)
        {
            var b = record[offset + i];
            if (b is >= 0x20 and < 0x7f) chars[count++] = (char)b;
        }
        return new string(chars[..count]).Trim();
    }
}
