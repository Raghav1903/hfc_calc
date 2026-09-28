using System.Text;
using HfcDesigner.Core.Formats;
using Xunit;

namespace HfcDesigner.Core.Tests;

public class FixedRecordScannerTests
{
    [Fact]
    public void Scan_DetectsRecordsAtNameOffsetAndHandlesBlankSlots()
    {
        const int recordLength = 64;
        const int nameOffset = 5;
        var data = new byte[LodeDataHeader.HeaderLength + recordLength * 4];

        WriteRecord(data, LodeDataHeader.HeaderLength + 0 * recordLength, nameOffset, "P3-500JCA-EX");
        WriteRecord(data, LodeDataHeader.HeaderLength + 1 * recordLength, nameOffset, "P3-500JCASS-EX");
        WriteRecord(data, LodeDataHeader.HeaderLength + 2 * recordLength, nameOffset, "P3-500JCA");
        // record index 3 is left all-zero to simulate a reserved/empty slot.

        var records = FixedRecordScanner.Scan(data, recordLength, nameOffset);

        Assert.Equal(4, records.Count);
        Assert.Equal("P3-500JCA-EX", records[0].Name);
        Assert.Equal("P3-500JCASS-EX", records[1].Name);
        Assert.Equal("P3-500JCA", records[2].Name);
        Assert.Equal("", records[3].Name);
        Assert.Equal(LodeDataHeader.HeaderLength, records[0].Offset);
        Assert.Equal(recordLength, records[0].Length);
    }

    [Fact]
    public void Scan_ReturnsEmptyWhenNoRecordPatternFound()
    {
        var data = new byte[LodeDataHeader.HeaderLength + 100];
        var records = FixedRecordScanner.Scan(data, recordLength: 64, nameOffsetInRecord: 5);

        Assert.Empty(records);
    }

    private static void WriteRecord(byte[] data, int recordStart, int nameOffset, string name)
    {
        var nameBytes = Encoding.ASCII.GetBytes(name);
        Array.Copy(nameBytes, 0, data, recordStart + nameOffset, nameBytes.Length);
    }
}
