# HFC Network Designer

A Windows desktop app (WPF, .NET 8) for opening and browsing Lode Data
**Design Assistant** network projects — `.NTW` network files plus their
associated spec files (`.PAR`, `.ATV`, `.TAP`, `.CPR`, `.CBL`, `.PRC`,
`.PER`, `.MGD`) — modeled after the workflow described at
https://docs.lodedata.com/design/manual/.

This is not affiliated with, and does not claim compatibility certified by,
Lode Data Corporation. It was built by inspecting real sample files and the
publicly available design manual; see `docs/FILE_FORMAT_NOTES.md` for
exactly what was confirmed versus inferred.

## Current scope

This first pass is a **project/file management shell**, not the full RF
design engine:

- **Project Settings** (File → Project Settings): configure the Network
  Folder and every spec file path the manual describes, plus Misc, Control
  File, and Report File folders. A **Set All Files** shortcut mirrors the
  real app's convenience feature — pick one `.PAR` file and every other
  spec path is filled in by swapping the extension, when your spec files
  share a common base name.
- Settings are saved as JSON with a `.hdproj` extension (see
  `docs/FILE_FORMAT_NOTES.md` for why this isn't a real `.DAP` file) and
  tracked in a small Recent Projects list.
- **Open Network (.NTW)**: parses the real Lode Data file header (title,
  design version, facility code, username), and de-obfuscates the file
  body (it's protected with a recoverable repeating-XOR pattern — see
  `docs/FILE_FORMAT_NOTES.md`) before showing any embedded text found in
  it. The actual schematic layout (nodes, branches, placed equipment)
  inside that de-obfuscated body is not decoded yet.
- **Spec file browsing**: `.CBL` (Cables) and `.ATV` (Actives) are parsed
  as real fixed-length record arrays — equipment names are extracted
  directly from the actual binary layout. `.PAR`, `.TAP`, `.CPR`, `.PRC`,
  `.PER`, and `.MGD` fall back to a raw text scan (their record layout is
  denser/table-shaped and hasn't been fully reverse-engineered yet — see
  `docs/FILE_FORMAT_NOTES.md`).

**Not implemented yet:** the schematic/canvas editor (Design, Active Entry,
Entry, and Powering modes), RF signal-level calculation, tap/pad/EQ
auto-selection, Bill of Materials, and reports. Those are a much larger
follow-on effort — see Roadmap below.

## Project layout

```
src/
  HfcDesigner.Core/     Class library: file format readers, project settings model
  HfcDesigner.App/      WPF application (net8.0-windows)
tests/
  HfcDesigner.Core.Tests/  xUnit tests for the format parsers (synthetic fixtures only)
docs/
  FILE_FORMAT_NOTES.md  Binary format reverse-engineering notes
```

## Building

`HfcDesigner.App` targets `net8.0-windows` with WPF, so it must be built on
Windows with the **.NET 8 SDK** and the **.NET desktop development**
workload (Visual Studio) or the `Microsoft.NET.Sdk.WindowsDesktop` MSBuild
target installed. `HfcDesigner.Core` and its tests are plain `net8.0` and
build cross-platform.

```powershell
dotnet build HfcDesigner.sln
dotnet test tests\HfcDesigner.Core.Tests
dotnet run --project src\HfcDesigner.App
```

> This code was written and reviewed in a Linux container without network
> access to the .NET SDK download host, so it has **not** been compiled or
> run here. Please build it on a Windows machine and report back if
> anything doesn't compile — the logic was hand-traced against the format
> notes above but not machine-verified end to end.

## Roadmap (not built yet)

1. **Decode the `.NTW` node/branch record layout.** The body is now
   correctly de-obfuscated (see `docs/FILE_FORMAT_NOTES.md`), and a
   repeating 261-byte record region is visible right after the header, but
   we don't yet know what any of those bytes mean. The realistic way to
   finish this is *differential* analysis: a brand-new empty network file,
   plus the same network with one element added with a known footage/cable
   type/house count, diffed byte-for-byte to see exactly what changed. If
   you can generate a few such controlled samples from the real Design
   Assistant, that would unblock this far faster than guessing.
2. Fully map the `.PAR`/`.TAP`/`.CPR` binary layouts (or obtain format docs
   from Lode Data) so those spec files get the same structured record view
   `.CBL`/`.ATV` already have, with real column names (attenuation, gain,
   loss values, etc.) instead of raw bytes.
3. Once (1) is solved, render a read-only network map from the decoded
   schematic data.
4. An actual Design/Entry/Powering editor with RF level calculation — a
   substantial project in its own right.
