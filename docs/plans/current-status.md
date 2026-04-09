# Current Status

## Last Updated

2026-04-08

## Current Phase

Phase 4: Tables

## Current Step

Step 4.5.4 — Border spacing handling — **COMPLETE**

Added table-level border spacing support (`w:tblCellSpacing`) by introducing
`TableElement.BorderSpacingTwips` and parsing it in `TableParser`.

Applied border spacing in layout calculations:
- `MeasureColumnWidths` now includes spacing inset per cell side
- `ComputeRowHeights` and width-aware row height calculation include vertical spacing
- width-aware row measurement subtracts spacing from available cell content width
- `LayoutCellContent` and `ComputeContentWidth` support spacing-aware calculations

Added focused tests in `TableParserTests` and `TableLayoutEngineTests` for spacing parsing,
defaults/fallbacks, and layout effects on content width and row heights.

1456 total tests passing in Release.

## Next Step

Step 4.5.5 — Render borders as line segments with appropriate width, color, and dash pattern

## Last Commit

Step 4.5.4: Handle border spacing

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
