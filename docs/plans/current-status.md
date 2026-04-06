# Current Status

## Last Updated

2026-04-06

## Current Phase

Phase 2: Text Layout

## Current Step

Step 2.1.2 — HarfBuzz shaping integration — **Complete**

- Added `ShapedGlyph` readonly record struct (Codepoint, AdvanceWidth, OffsetX, OffsetY, Cluster)
- Added `ShapedGlyphRun` class (Glyphs list + TotalWidth)
- Added `MeasurementEngine.ShapeText()` method wrapping `SKShaper`:
  - validates inputs (null guards, non-positive font size)
  - shapes text via HarfBuzz producing glyph-level results
  - computes per-glyph advance widths from position deltas
  - preserves cluster mapping back to source text
- 12 new tests covering: null guards, non-positive size, empty text,
  positive advances, total width match, advance sum, cluster range,
  whitespace glyphs, codepoint non-zero
- 278 total tests passing, 100% line coverage maintained

## Next Step

Step 2.1.3 — Handle measurement in twips: all measurements returned in twips; conversion to output units deferred to render time

## Last Commit

5b6c1a0 — Implement step 2.1.1: add measurement engine

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
