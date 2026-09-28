namespace HfcDesigner.Core.Formats;

/// <summary>Opens a Lode Data spec/network file and picks the right parser for its kind.</summary>
public static class SpecFileReader
{
    public static SpecFileDocument Open(string filePath, SpecFileKind? kindHint = null)
    {
        var data = File.ReadAllBytes(filePath);
        var kind = kindHint
            ?? SpecFileKindExtensions.FromExtension(Path.GetExtension(filePath))
            ?? throw new InvalidDataException(
                $"Could not determine the Lode Data file type of '{filePath}' from its extension.");

        var header = LodeDataHeader.Parse(data);

        return kind switch
        {
            // Confirmed flat fixed-length record arrays (see FixedRecordScanner).
            SpecFileKind.Cables => BuildStructured(kind, filePath, header, data, recordLength: 384, nameOffset: 5),
            SpecFileKind.Actives => BuildStructured(kind, filePath, header, data, recordLength: 318, nameOffset: 0),

            // Everything else uses denser table/matrix layouts we haven't mapped yet.
            _ => BuildRaw(kind, filePath, header, data),
        };
    }

    private static SpecFileDocument BuildStructured(
        SpecFileKind kind, string path, LodeDataHeader header, byte[] data, int recordLength, int nameOffset)
    {
        var records = FixedRecordScanner.Scan(data, recordLength, nameOffset);
        return new SpecFileDocument
        {
            Kind = kind,
            FilePath = path,
            Header = header,
            IsStructured = true,
            Records = records,
            FileSizeBytes = data.LongLength,
        };
    }

    private static SpecFileDocument BuildRaw(SpecFileKind kind, string path, LodeDataHeader header, byte[] data)
    {
        // .NTW bodies are obfuscated with a recoverable repeating XOR key
        // (see NtwBodyCipher); everything else is scanned as-is.
        if (kind != SpecFileKind.Network)
        {
            var tokens = RawTokenScanner.Scan(data);
            return new SpecFileDocument
            {
                Kind = kind,
                FilePath = path,
                Header = header,
                IsStructured = false,
                RawTokens = tokens,
                FileSizeBytes = data.LongLength,
            };
        }

        var body = data[LodeDataHeader.HeaderLength..];
        var decrypted = NtwBodyCipher.Decrypt(body);
        var bodyTokens = RawTokenScanner.Scan(decrypted.Plaintext, startOffset: 0)
            .Select(t => t with { Offset = t.Offset + LodeDataHeader.HeaderLength })
            .ToList();

        return new SpecFileDocument
        {
            Kind = kind,
            FilePath = path,
            Header = header,
            IsStructured = false,
            RawTokens = bodyTokens,
            FileSizeBytes = data.LongLength,
            BodyWasDecrypted = decrypted.WasDecrypted,
            DecryptionKeyPeriod = decrypted.Period,
        };
    }
}
