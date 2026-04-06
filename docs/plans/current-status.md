# Current Status

## Last Updated

2026-04-06

## Current Phase

Phase 2: Text Layout

## Current Step

Step 2.1.4 — Character metrics for superscript/subscript — **Complete**

- Added `CharacterMetrics` readonly record struct (AdvanceWidth, Ascent, Descent, Leading, LineHeight)
- Added `MeasurementEngine.MeasureCharacter()` — measures a single character’s advance width plus font metrics (ascent/descent/leading/line height) using `SKFont.Metrics`
- Added `MeasurementEngine.MeasureCharacterInTwips()` — same but all values in twips
- SKFontMetrics.Ascent is negative (upward), normalised to positive in `CharacterMetrics`
- 12 new tests covering: null guard, non-positive size, positive advance/ascent/descent, non-negative leading, LineHeight consistency, cross-check with MeasureGlyphAdvances, font-size scaling, space character, twip scaling
- 311 total tests passing, 100% line coverage maintained

## Next Step

Step 2.1.5 — Unit tests: verify measurements for known fonts produce expected widths (tolerance: ±1 twip)

## Last Commit

8acfc9e — Implement step 2.1.3: handle measurement in twips

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
