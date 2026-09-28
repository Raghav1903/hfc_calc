using System.Text;
using HfcDesigner.Core.Formats;
using Xunit;

namespace HfcDesigner.Core.Tests;

public class LodeDataHeaderTests
{
    // These fixtures are hand-built to match the layout confirmed by inspecting
    // real Lode Data files (see docs/FILE_FORMAT_NOTES.md) — no proprietary
    // customer data is embedded here. Bytes 0x80 and 0x90 are left as the
    // zero-initialized reserved byte real files have there; the code and
    // username strings start one byte later, at 0x81 and 0x91.
    private static byte[] BuildHeader(string title, string? version, string code, string userName)
    {
        var buffer = new byte[LodeDataHeader.HeaderLength];
        WriteAscii(buffer, 0, title);
        if (version is not null) WriteAscii(buffer, 0x1c, "Design " + version);
        WriteAscii(buffer, 0x81, code);
        WriteAscii(buffer, 0x91, userName);
        return buffer;
    }

    private static void WriteAscii(byte[] buffer, int offset, string text)
    {
        var bytes = Encoding.ASCII.GetBytes(text);
        Array.Copy(bytes, 0, buffer, offset, bytes.Length);
    }

    [Fact]
    public void Parse_ReadsTitleCodeAndUserName()
    {
        var buffer = BuildHeader("Lode Data Cables File", null, "CATL0WNPG4DVBH2", "mhornber");
        var header = LodeDataHeader.Parse(buffer);

        Assert.Equal("Lode Data Cables File", header.Title);
        Assert.Equal("CATL0WNPG4DVBH2", header.Code);
        Assert.Equal("mhornber", header.UserName);
        Assert.Null(header.DesignVersion);
    }

    [Fact]
    public void Parse_ReadsDesignVersionWhenPresent()
    {
        var buffer = BuildHeader("Lode Data Network File", "12.11", "LP-990XYP3-1", "CCJ");
        var header = LodeDataHeader.Parse(buffer);

        Assert.Equal("Design 12.11", header.DesignVersion);
    }

    [Fact]
    public void Parse_ThrowsWhenFileTooShort()
    {
        var buffer = new byte[10];
        Assert.Throws<InvalidDataException>(() => LodeDataHeader.Parse(buffer));
    }
}
