using System.Text;

namespace HfcDesigner.Core.Formats;

/// <summary>
/// Fallback reader for spec file formats whose record layout has not been
/// reverse-engineered yet. Real .PAR, .TAP and .CPR files use denser,
/// table/matrix-shaped bodies (lookup tables, cross-reference grids) rather
/// than a flat record array, so instead of guessing field offsets this just
/// extracts every run of printable ASCII text, which is enough to browse a
/// file's contents (part numbers, model names, level/pedestal labels, etc.)
/// without claiming to know the numeric layout around them.
/// </summary>
public static class RawTokenScanner
{
    public static IReadOnlyList<RawToken> Scan(byte[] data, int startOffset = LodeDataHeader.HeaderLength, int minLength = 3)
    {
        var tokens = new List<RawToken>();
        var i = Math.Max(startOffset, 0);

        while (i < data.Length)
        {
            if (IsPrintable(data[i]))
            {
                var start = i;
                var sb = new StringBuilder();
                while (i < data.Length && IsPrintable(data[i]))
                {
                    sb.Append((char)data[i]);
                    i++;
                }

                var text = sb.ToString().Trim();
                if (text.Length >= minLength)
                {
                    tokens.Add(new RawToken(start, text));
                }
            }
            else
            {
                i++;
            }
        }

        return tokens;
    }

    private static bool IsPrintable(byte b) => b is >= 0x20 and < 0x7f;
}
