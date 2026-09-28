namespace HfcDesigner.Core.Formats;

/// <summary>
/// The body of a real .NTW file is obfuscated with a simple repeating-key
/// XOR. Comparing a real 254 KB network file's body to itself shifted by N
/// bytes, for N=100 (and its harmonics 200, 300, ...) about 94% of bytes
/// matched, versus ~2% for any other candidate shift, including sub-periods
/// like 50, 25, 20, 10, 5, and 4 — far too strong, and too clearly a single
/// fundamental period, to be coincidental.
///
/// Because the underlying plaintext is mostly zero-filled unused space
/// (most of a preallocated network structure is empty), the repeating key
/// can be recovered without knowing it in advance: for each of the
/// `period` key-byte positions, the most common ciphertext byte at that
/// position (its statistical mode) is almost always `0 XOR key[i] ==
/// key[i]`, since the true plaintext is 0 there far more often than not.
/// This is not real encryption (no secret is required to reverse it) — it
/// reads like simple obfuscation against casual inspection/tampering.
///
/// This has only been confirmed against .NTW files. It has not been tested
/// against the other Lode Data formats, which is why <see cref="SpecFileReader"/>
/// only applies it to <see cref="SpecFileKind.Network"/>.
/// </summary>
public static class NtwBodyCipher
{
    private const int MinPeriod = 8;
    private const int MaxPeriod = 256;
    private const int MaxScanBytes = 200_000;

    /// <summary>
    /// A real repeating-key match ratio (~0.94 in our sample) is far above
    /// the ~1/256 chance level for unrelated random bytes. 0.5 is a
    /// deliberately conservative cutoff so an unobfuscated file (where this
    /// pattern simply isn't detected) is passed through unchanged rather
    /// than mangled by a false-positive "decryption".
    /// </summary>
    private const double MinConfidentRatio = 0.5;

    public static NtwBodyDecryptResult Decrypt(byte[] body)
    {
        var period = DetectPeriod(body);
        if (period is null)
        {
            return new NtwBodyDecryptResult(false, null, Array.Empty<byte>(), body);
        }

        var key = RecoverKey(body, period.Value);
        var plaintext = new byte[body.Length];
        for (var i = 0; i < body.Length; i++)
        {
            plaintext[i] = (byte)(body[i] ^ key[i % period.Value]);
        }

        return new NtwBodyDecryptResult(true, period, key, plaintext);
    }

    private static int? DetectPeriod(byte[] body)
    {
        var scanLength = Math.Min(body.Length, MaxScanBytes);
        var bestPeriod = -1;
        var bestRatio = 0.0;

        for (var period = MinPeriod; period <= MaxPeriod && period < scanLength; period++)
        {
            var total = scanLength - period;
            if (total <= 0) continue;

            var matches = 0;
            for (var i = 0; i < total; i++)
            {
                if (body[i] == body[i + period]) matches++;
            }

            var ratio = (double)matches / total;
            if (ratio > bestRatio)
            {
                bestRatio = ratio;
                bestPeriod = period;
            }
        }

        return bestRatio >= MinConfidentRatio ? bestPeriod : null;
    }

    private static byte[] RecoverKey(byte[] body, int period)
    {
        var key = new byte[period];
        var counts = new int[256];

        for (var i = 0; i < period; i++)
        {
            Array.Clear(counts);
            for (var j = i; j < body.Length; j += period)
            {
                counts[body[j]]++;
            }

            var best = 0;
            for (var v = 1; v < 256; v++)
            {
                if (counts[v] > counts[best]) best = v;
            }
            key[i] = (byte)best;
        }

        return key;
    }
}

/// <param name="WasDecrypted">
/// False when the repeating-XOR pattern wasn't confidently detected —
/// <see cref="Plaintext"/> is then just the original bytes, unchanged.
/// </param>
/// <param name="Period">The recovered key length in bytes, or null if not detected.</param>
/// <param name="Key">The recovered repeating XOR key, or empty if not detected.</param>
/// <param name="Plaintext">The de-obfuscated body (or the original body, if not detected).</param>
public sealed record NtwBodyDecryptResult(bool WasDecrypted, int? Period, byte[] Key, byte[] Plaintext);
