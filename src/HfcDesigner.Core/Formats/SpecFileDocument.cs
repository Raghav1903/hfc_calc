namespace HfcDesigner.Core.Formats;

/// <summary>The parsed contents of one Lode Data spec/network file.</summary>
public sealed class SpecFileDocument
{
    public required SpecFileKind Kind { get; init; }
    public required string FilePath { get; init; }
    public required LodeDataHeader Header { get; init; }

    /// <summary>
    /// True when <see cref="Records"/> came from a known fixed-length record
    /// layout (currently .CBL and .ATV). False means the format's record
    /// layout isn't mapped yet and <see cref="RawTokens"/> should be used
    /// instead — see <see cref="RawTokenScanner"/>.
    /// </summary>
    public required bool IsStructured { get; init; }

    public IReadOnlyList<SpecFileRecord> Records { get; init; } = Array.Empty<SpecFileRecord>();
    public IReadOnlyList<RawToken> RawTokens { get; init; } = Array.Empty<RawToken>();
    public long FileSizeBytes { get; init; }

    /// <summary>
    /// True for .NTW files whose body was de-obfuscated with a recovered
    /// repeating-XOR key before <see cref="RawTokens"/> was extracted from
    /// it — see <see cref="NtwBodyCipher"/>. Always false for other kinds.
    /// </summary>
    public bool BodyWasDecrypted { get; init; }

    /// <summary>The recovered XOR key length in bytes, when <see cref="BodyWasDecrypted"/> is true.</summary>
    public int? DecryptionKeyPeriod { get; init; }
}
