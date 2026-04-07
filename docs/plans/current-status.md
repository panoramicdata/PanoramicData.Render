# Current Status

## Last Updated

2026-04-07

## Current Phase

Phase 2: Text Layout

## Current Step

Step 2.3.1 — Alignment: Left, Right, Center, Justified — compute X offsets for each glyph run per line — **Complete**

- Created `ParagraphAlignment` enum (Left, Center, Right, Justified)
- Created `PositionedBox` record struct (ItemIndex, XOffset, Width)
- Created `ParagraphAligner` static class with `ComputeBoxPositions` method:
  - Takes items, line, lineWidth, alignment
  - Left: boxes start at X=0
  - Center: boxes offset by (lineWidth - contentWidth) / 2
  - Right: boxes offset by lineWidth - contentWidth
  - Justified: glue widths adjusted using KP adjustment ratio (stretch/shrink)
  - Flagged penalty breaks add hyphen box at end of line
  - Overfull lines clamp center/right offset to 0
- Added 25 new tests covering guards, all 4 alignment modes, flagged/non-flagged penalties,
  edge cases (empty line, overfull content, non-zero start index, mid-line penalty)
- 481 total tests passing, 100% line coverage maintained

## Next Step

Step 2.3.2 — Justification: Distribute extra whitespace across glue items on justified lines; do not justify the last line

## Last Commit

217bd3d — Implement step 2.2.7: Verify break positions against hand-computed expected results

## Implementation Notes

- `DocxDocument` is internal; test project accesses it via `InternalsVisibleTo`
- `TestDocxBuilder` helper creates minimal and full DOCX files in-memory for tests
- `DocxDocument.Load` disposes the underlying `WordprocessingDocument` on constructor failure (no resource leak)
- Overfull line penalty is 100M demerits to strongly prefer breaking over overflowing
- When no stretch available (ratio > tolerance), accepted with 10K extra demerits
- Application Control policy may block Debug DLLs on this machine; use Release for coverage
- Renamed `HeaderFooterType` to `HeaderFooterKind` to avoid collision with `DocumentFormat.OpenXml.Wordprocessing.HeaderFooterType`
- OpenXML `EnumValue<T>` types cannot be used in C# switch patterns; use `if` chains with `==` instead
- Using TDD + spec-driven development from this point forward

## Blockers

None.

## Decisions Made

- .NET 10.0 target (not .NET 8.0 or .NET Standard 2.1 from original brief)
- Knuth-Plass line breaking from day 1 (not greedy)
- Full OOXML style cascade including toggle properties
- High-fidelity goal (not pixel-perfect)
- SkiaSharp for measurement and PDF — accepting known limitations (no tagged PDF, no font subsetting, no PDF/A)
- Library only, no CLI tool
- DOCX only, never .doc
- No macro support
- Visual regression testing: test project may use Word Interop (Microsoft.Office.Interop.Word) to generate ground-truth PNGs for comparison; the main library must NEVER reference Word Interop
