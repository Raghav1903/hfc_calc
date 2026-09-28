namespace HfcDesigner.Core.Formats;

/// <summary>The Lode Data Design Assistant file types this app knows about.</summary>
public enum SpecFileKind
{
    Network,     // .NTW
    Parameters,  // .PAR
    Actives,     // .ATV
    Taps,        // .TAP
    Couplers,    // .CPR
    Cables,      // .CBL
    Pricing,     // .PRC
    Performance, // .PER
    MapGrid,     // .MGD
}

public static class SpecFileKindExtensions
{
    public static string ToExtension(this SpecFileKind kind) => kind switch
    {
        SpecFileKind.Network => ".NTW",
        SpecFileKind.Parameters => ".PAR",
        SpecFileKind.Actives => ".ATV",
        SpecFileKind.Taps => ".TAP",
        SpecFileKind.Couplers => ".CPR",
        SpecFileKind.Cables => ".CBL",
        SpecFileKind.Pricing => ".PRC",
        SpecFileKind.Performance => ".PER",
        SpecFileKind.MapGrid => ".MGD",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    public static string DisplayName(this SpecFileKind kind) => kind switch
    {
        SpecFileKind.Network => "Network",
        SpecFileKind.Parameters => "Parameters",
        SpecFileKind.Actives => "Actives",
        SpecFileKind.Taps => "Taps",
        SpecFileKind.Couplers => "Couplers",
        SpecFileKind.Cables => "Cables",
        SpecFileKind.Pricing => "Pricing",
        SpecFileKind.Performance => "Performance",
        SpecFileKind.MapGrid => "Map Grid",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    public static SpecFileKind? FromExtension(string extension)
    {
        return extension.TrimStart('.').ToUpperInvariant() switch
        {
            "NTW" => SpecFileKind.Network,
            "PAR" => SpecFileKind.Parameters,
            "ATV" => SpecFileKind.Actives,
            "TAP" => SpecFileKind.Taps,
            "CPR" => SpecFileKind.Couplers,
            "CBL" => SpecFileKind.Cables,
            "PRC" => SpecFileKind.Pricing,
            "PER" => SpecFileKind.Performance,
            "MGD" => SpecFileKind.MapGrid,
            _ => null,
        };
    }
}
