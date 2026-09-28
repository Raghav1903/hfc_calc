# Lode Data file format notes

This documents what was learned by hex-inspecting real Lode Data Design
Assistant files (`.NTW`, `.PAR`, `.ATV`, `.TAP`, `.CPR`, `.CBL`) provided for
this project, so the parsing logic in `HfcDesigner.Core` has a written
rationale instead of being a black box. No proprietary sample files are
checked into this repository — the byte offsets below were derived from
files outside the repo and are re-verified in `HfcDesigner.Core.Tests`
against small hand-built fixtures.

**This is not an official specification.** Lode Data has not published the
binary layout of these files; everything here is inferred from a handful of
sample files and should be treated as a working hypothesis, not ground
truth. Corrupt or unusually old files may not follow it.

## Common 512-byte header (all file types)

Every file we inspected — `.NTW`, `.PAR`, `.ATV`, `.TAP`, `.CPR`, `.CBL`, and
presumably `.PRC`/`.PER`/`.MGD` — starts with the same 0x200 (512) byte
header:

| Offset | Field | Notes |
|---|---|---|
| `0x000` | Title | Null-terminated ASCII, e.g. `"Lode Data Cables File"`. Identifies the file type independent of its extension. |
| `0x01C`ish | `"Design x.xx"` | Present in some files (seen in `.CPR`, `.NTW`), absent in others. Found by scanning for the literal text `"Design "` within the first 0x80 bytes rather than assuming a fixed offset. |
| `0x080` | (reserved) | A single byte, `0x00` in every sample inspected. Purpose unknown — possibly a flag or a length-prefix byte that just happens to be unused/zero in our samples. |
| `0x081` | Code | Null-terminated ASCII. A facility/system code in spec files; appears to hold a node/headend name in `.NTW` files. |
| `0x090` | (reserved) | Same as `0x080`, always `0x00` in our samples. |
| `0x091` | User name | Null-terminated ASCII. The Windows username of whoever last saved the file (e.g. `"mhornber"`), or the designer's initials in `.NTW` files. |
| `0x200`+ | Body | Format-specific record data — see below. |

An earlier version of this doc (and the code) had these two fields starting
at `0x080`/`0x090` — an off-by-one from misreading a hex dump, where the
label on a dump row is the offset of its *first* byte, not of the first
*visible character* in that row. That byte is always `0x00`, so the bug
made the Code and User/Node fields render blank in the app. Fixed after a
user reported it from a real run.

Implemented in `HfcDesigner.Core/Formats/LodeDataHeader.cs`.

## Structured (flat fixed-length record array) formats

Two of the formats we inspected are simply an array of fixed-length records
starting right after the header, each containing a null-terminated ASCII
name:

| Extension | Record length | Name offset within record | Confirmed by |
|---|---|---|---|
| `.CBL` (Cables) | 384 bytes (`0x180`) | 5 | 3+ consecutive records at offset `0x200`, `0x380`, `0x500`, ... each starting `XX 00 00 00 00 <name>` |
| `.ATV` (Actives) | 318 bytes (`0x13E`) | 0 | 12 consecutive occurrences of an active model name string exactly 318 bytes apart |

`FixedRecordScanner` reads these generically: it auto-detects the first
record offset by scanning for three consecutive record-length strides that
each start with two uppercase-letter/digit bytes (equipment codes like
`P3-500JCA-EX` or `BLE120S` always start this way; the third character is
often a dash or space, so only the first two bytes are checked). This means
it isn't hardcoded to the exact preamble size seen in our sample files.

**Only the name field is decoded.** The remaining bytes in each record
almost certainly encode attenuation, gain, and other RF parameters as
32-bit values (some fields repeat identically twice per record in the
`.CBL` samples, suggesting duplicated forward/return or high/low-band
values), but we have not mapped which bytes mean what. `SpecFileRecord`
exposes the full raw record bytes (`RawData`) for future work.

## Table/matrix formats (not yet structurally parsed)

`.PAR` (Parameters), `.TAP` (Taps), and `.CPR` (Couplers) do **not** use a
flat record array. Inspection showed:

- `.PAR` has a small lookup-table region right after the header (index
  bytes, then a list of category labels like `HSG TO HSG`, `VOID`,
  `TERMINATOR`, `TAP PED`) rather than one record per item. This matches the
  Design Assistant manual's description of the Parameters file as multiple
  tabs of global settings (crossover limits, tap windows, pedestal part
  numbers, 16 signal-level presets, NIU powering settings, etc.) rather than
  a list of named equipment.
- `.TAP` has a repeating preamble block before the first named tap entry,
  and named entries are not evenly spaced — tap "families" (e.g. `FFT2-29P`,
  `FFT4-29P`, `FFT8-29P`) appear to pack multiple port-count variants into
  a shared structure rather than one fixed-length record per name.
- `.CPR` starts immediately after the header with a small binary header
  followed by a row of space-padded coupler model names concatenated
  together (column headers for a loss/coupling-value matrix), not a
  per-item record list.

Byte-perfect parsing of these would need substantially more reverse
engineering (or real format documentation from Lode Data) than is in scope
for this pass. Rather than guess at field offsets, `RawTokenScanner`
extracts every run of printable ASCII text from the file body, which is
enough to browse a file's contents (model/part names, labels) without
claiming to know the surrounding numeric layout. `.PRC`, `.PER`, and `.MGD`
use the same fallback since no samples of those were available to inspect
at all.

## `.NTW` (Network) files

Same 512-byte header. The body looked like meaningless noise at first — a
printable-string scan turned up nothing but garbage like `J~w8!` and
`@v+FQ</q`. That's because **the body is obfuscated with a simple
repeating-key XOR**, not because the underlying data is unreadable.

### Finding the obfuscation

Comparing a real 254 KB network file's body to itself shifted by increasing
distances showed a very sharp, isolated peak: shifting by 100 bytes (and
its harmonics 200, 300, ...) made ~94% of bytes match, while every other
distance tested — including sub-periods of 100 like 50, 25, 20, 10, 5, and
4 — matched only ~2-3% of the time (consistent with unrelated random
bytes). That kind of gap is not something that happens by chance; it means
the body repeats with a genuine 100-byte period.

### Recovering the key without knowing it

Because a Lode Data network file preallocates a large, mostly-empty
structure (see below), the *plaintext* underneath is zero almost
everywhere. That makes the repeating key recoverable with no prior
knowledge: for each of the 100 key-byte positions, take the most common
ciphertext byte seen at that position across the whole file (its
statistical mode). Since plaintext is 0 there far more often than not,
`0 XOR key[i] == key[i]`, so the mode is almost always the real key byte.
Applying that recovered key turns the ~94%-repeating noise into a body
that is **97% literal zero bytes**, with small, clearly-structured
non-zero clusters — a very different (and far more promising-looking)
picture than the raw ciphertext.

This is implemented generically in `NtwBodyCipher`: it scans candidate key
lengths from 8 to 256 bytes, picks whichever one shows a self-similarity
ratio of at least 50% (comfortably above chance, comfortably below the
~94% a real match shows), and recovers the key by per-position mode. If no
period clears that bar, the body is passed through unchanged rather than
"decrypting" into worse noise. This is obfuscation, not real
cryptography — no secret is needed to reverse it, and it may well be a
side effect of however the on-disk format is packed rather than an
intentional protection measure.

### What's visible after decoding, and what's still unknown

Right after the header, the de-obfuscated body starts with several
261-byte-apart entries, each holding about 19-20 bytes of real (non-zero)
data followed by zero padding — for example the first one decodes to
`1c cc ec 7d 05 03 0d 0d 34 bc 4c 0d c4 bc 94 ac 47 3c 7c`. These don't
look like text (no recognizable ASCII), which fits the Design Assistant
manual's description of per-node fields as small packed numbers (footage,
house count, cable ID, level ID, coupler ID, branch pointers, etc.) rather
than strings. Past the first handful of these evenly-spaced entries the
non-zero clusters become sparser and irregularly spaced, which likely
reflects the real (sparse) network topology rather than a fixed-size
record array — but we don't have a way to confirm that without either
official format documentation or a set of *controlled* sample files (e.g.
a brand-new empty network, then the same network with one added element of
a known footage/cable/house count, so a byte-diff shows exactly which
bytes changed and what they mean). Full schematic parsing (nodes,
branches, cable spans, actives/taps/couplers placed in the network) is not
implemented — this is the next real step, see the Roadmap section of the
root `README.md`.

## Why project settings aren't a real `.DAP` file

The Design Assistant manual describes a `.DAP` ("Design Assistant Project")
file storing the folder/file paths configured in **File → Project
Settings**. We were not given a sample `.DAP` file, so its format is
unknown. `HfcDesigner.Core.Project.ProjectSettings` models the same set of
paths the manual describes, but is persisted as plain JSON with a `.hdproj`
extension rather than claiming binary compatibility with `.DAP`.
