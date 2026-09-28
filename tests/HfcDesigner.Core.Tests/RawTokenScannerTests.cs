using System.Text;
using HfcDesigner.Core.Formats;
using Xunit;

namespace HfcDesigner.Core.Tests;

public class RawTokenScannerTests
{
    [Fact]
    public void Scan_ExtractsPrintableRunsAboveMinLength()
    {
        var data = new byte[LodeDataHeader.HeaderLength + 32];
        var text = Encoding.ASCII.GetBytes("HSG TO HSG");
        Array.Copy(text, 0, data, LodeDataHeader.HeaderLength + 4, text.Length);

        var tokens = RawTokenScanner.Scan(data);

        Assert.Contains(tokens, t => t.Text == "HSG TO HSG");
    }

    [Fact]
    public void Scan_SkipsTokensShorterThanMinLength()
    {
        var data = new byte[LodeDataHeader.HeaderLength + 16];
        var text = Encoding.ASCII.GetBytes("ab");
        Array.Copy(text, 0, data, LodeDataHeader.HeaderLength + 4, text.Length);

        var tokens = RawTokenScanner.Scan(data, minLength: 3);

        Assert.DoesNotContain(tokens, t => t.Text == "ab");
    }

    [Fact]
    public void Scan_IgnoresBytesBeforeStartOffset()
    {
        var data = new byte[LodeDataHeader.HeaderLength + 16];
        var text = Encoding.ASCII.GetBytes("SECRET");
        Array.Copy(text, 0, data, 10, text.Length); // inside the header region

        var tokens = RawTokenScanner.Scan(data);

        Assert.DoesNotContain(tokens, t => t.Text == "SECRET");
    }
}
