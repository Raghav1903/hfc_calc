using HfcDesigner.Core.Formats;
using Xunit;

namespace HfcDesigner.Core.Tests;

public class NtwBodyCipherTests
{
    [Fact]
    public void Decrypt_RecoversRepeatingKeyFromMostlyZeroPlaintext()
    {
        const int period = 20;
        var key = new byte[period];
        for (var i = 0; i < period; i++) key[i] = (byte)(0x40 + i);

        // Build a plaintext that's almost all zero (like a real network
        // file's mostly-unused preallocated space) with a handful of real
        // non-zero bytes scattered in, then XOR-encrypt it with the key —
        // this mirrors the structure that made key recovery possible on
        // real files.
        var plaintext = new byte[period * 500];
        plaintext[7] = 0x11;
        plaintext[2007] = 0x22;
        plaintext[9013] = 0x33;

        var ciphertext = new byte[plaintext.Length];
        for (var i = 0; i < plaintext.Length; i++)
        {
            ciphertext[i] = (byte)(plaintext[i] ^ key[i % period]);
        }

        var result = NtwBodyCipher.Decrypt(ciphertext);

        Assert.True(result.WasDecrypted);
        Assert.Equal(period, result.Period);
        Assert.Equal(plaintext, result.Plaintext);
    }

    [Fact]
    public void Decrypt_LeavesRandomLookingDataUnchangedWhenNoPeriodDetected()
    {
        var random = new Random(12345);
        var body = new byte[10_000];
        random.NextBytes(body);

        var result = NtwBodyCipher.Decrypt(body);

        Assert.False(result.WasDecrypted);
        Assert.Null(result.Period);
        Assert.Equal(body, result.Plaintext);
    }
}
