# Current Status

## Last Updated

2026-04-06

## Current Phase

Phase 2: Text Layout

## Current Step

Step 2.1.3 — Handle measurement in twips — **Complete**

- Added `TwipConverter` static utility class:
  - `PointsToTwips` / `TwipsToPoints` (1 pt = 20 twips)
  - `InchesToTwips` / `TwipsToInches` (1 in = 1440 twips)
  - `TwipsToPixels` (twips × DPI / 1440)
- Added `MeasurementEngine.MeasureGlyphAdvancesInTwips()` — per-character advances in twips
- Added `MeasurementEngine.ShapeTextInTwips()` — shaped glyph run with all values in twips
- 21 new tests (15 TwipConverter + 6 MeasurementEngine twip variants)
- 299 total tests passing, 100% line coverage maintained

## Next Step

Step 2.1.4 — Measure individual characters for superscript/subscript offset calculations

## Last Commit

60f8df3 — Implement step 2.1.2: integrate HarfBuzz shaping via SKShaper

## Implementation Notes

- `DocxDocument` is internal; test project accesses it via `InternalsVisibleTo`
- `TestDocxBuilder` helper creates minimal and full DOCX files in-memory for tests
- `DocxDocument.Load` disposes the underlying `WordprocessingDocument` on constructor failure (no resource leak)
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
