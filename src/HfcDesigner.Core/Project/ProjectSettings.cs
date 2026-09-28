using HfcDesigner.Core.Formats;

namespace HfcDesigner.Core.Project;

/// <summary>
/// The set of folder/file paths the Design Assistant calls "Project
/// Settings" (File → Project Settings, saved as a .DAP file in the real
/// app): where network files live, where each spec file is, and where
/// misc/control/report output goes. See docs/FILE_FORMAT_NOTES.md for why
/// this app stores the equivalent settings as JSON (.hdproj) instead of
/// the real .DAP binary format.
/// </summary>
public sealed class ProjectSettings
{
    public string NetworkFolder { get; set; } = "";

    public string ParametersFile { get; set; } = "";  // .PAR
    public string ActivesFile { get; set; } = "";      // .ATV
    public string TapsFile { get; set; } = "";         // .TAP
    public string CouplersFile { get; set; } = "";     // .CPR
    public string CablesFile { get; set; } = "";       // .CBL
    public string PricingFile { get; set; } = "";      // .PRC
    public string PerformanceFile { get; set; } = "";  // .PER
    public string MapGridFile { get; set; } = "";      // .MGD

    public string MiscFolder { get; set; } = "";
    public string ControlFileFolder { get; set; } = "";
    public string ReportFileFolder { get; set; } = "";

    public IEnumerable<(SpecFileKind Kind, string Path)> EnumerateSpecFiles()
    {
        yield return (SpecFileKind.Parameters, ParametersFile);
        yield return (SpecFileKind.Actives, ActivesFile);
        yield return (SpecFileKind.Taps, TapsFile);
        yield return (SpecFileKind.Couplers, CouplersFile);
        yield return (SpecFileKind.Cables, CablesFile);
        yield return (SpecFileKind.Pricing, PricingFile);
        yield return (SpecFileKind.Performance, PerformanceFile);
        yield return (SpecFileKind.MapGrid, MapGridFile);
    }
}
