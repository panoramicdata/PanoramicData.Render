# Current Status

## Last Updated

2026-04-06

## Current Phase

Phase 2: Text Layout

## Current Step

Step 2.1.5 — Known-font verification tests — **Complete**

- Added `KnownFontMeasurementTests` class (18 tests) using Arial specifically:
  - Per-character twip advances match direct SkiaSharp measurement (±1 twip)
  - Shaped total width matches direct HarfBuzz (±1 twip)
  - Shaped glyph advances match direct HarfBuzz (±1 twip)
  - Character metrics (ascent/descent/leading) match SKFontMetrics (±1 twip)
  - 10 font sizes (8–48pt) verified for pangram text
  - Shaped vs unshaped total width cross-validation (5% tolerance for kerning)
  - Line height consistency across characters
  - Superscript simulation: 2/3 size ratio verified
  - Full alphanumeric + space: MeasureCharacter matches MeasureGlyphAdvances (±1 twip)
- Tests skip gracefully via `Assert.Skip()` on non-Windows platforms
- 329 total tests passing, 100% line coverage maintained

## Next Step

Step 2.2.1 — Implement the Knuth-Plass optimal paragraph-breaking algorithm: box, glue, penalty model

## Last Commit

135d622 — Implement step 2.1.4: measure individual characters for superscript/subscript

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
