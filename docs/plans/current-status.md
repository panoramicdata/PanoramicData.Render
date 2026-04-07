# Current Status

## Last Updated

2026-04-07

## Current Phase

Phase 2: Text Layout

## Current Step

Step 2.3.2 — Justification: Distribute extra whitespace; do not justify the last line — **Complete**

- Added `isLastLine` parameter to `ComputeBoxPositions` (default: false)
  - When true + Justified alignment: treats line as Left-aligned (natural glue widths)
  - No effect on Left, Center, Right alignments
- Added `ComputeParagraphBoxPositions` convenience method:
  - Takes items, all lines, lineWidth, alignment
  - Automatically detects last line and passes `isLastLine=true`
  - Returns `IReadOnlyList<IReadOnlyList<PositionedBox>>` — one list per line
- Added 14 new tests:
  - Last-line justified → natural glue (not adjusted)
  - Not-last-line justified → adjusted glue
  - Last-line on Left/Center/Right → no effect
  - Paragraph-level: null guards, empty lines, single line, two lines, three lines
  - Center alignment paragraph-level
- 495 total tests passing, 100% line coverage maintained

## Next Step

Step 2.3.3 — Indentation: First-line indent, hanging indent, left margin, right margin

## Last Commit

23c6870 — Implement step 2.3.1: Alignment — compute X offsets for each glyph run per line

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
