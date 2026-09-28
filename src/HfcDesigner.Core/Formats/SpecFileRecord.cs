namespace HfcDesigner.Core.Formats;

/// <summary>
/// One fixed-length record from a structured spec file body (currently
/// .CBL and .ATV — see <see cref="FixedRecordScanner"/>). <see cref="RawData"/>
/// holds the full record bytes; only the name has been decoded so far, the
/// remaining numeric fields (attenuation, gain, etc.) have not been mapped.
/// </summary>
public sealed record SpecFileRecord(int Index, long Offset, string Name, byte[] RawData)
{
    public int Length => RawData.Length;
}

/// <summary>
/// A run of printable ASCII text found while scanning a spec file body whose
/// record layout hasn't been reverse-engineered yet — see
/// <see cref="RawTokenScanner"/>.
/// </summary>
public sealed record RawToken(long Offset, string Text);
