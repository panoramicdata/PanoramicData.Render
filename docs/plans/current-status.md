# Current Status

## Last Updated

2026-04-07

## Current Phase

Phase 2: Text Layout

## Current Step

Step 2.4.1 — Font properties: Family, size, bold, italic — select correct SKTypeface — **Complete**

- Created `FontProperties` readonly record struct:
  - `FamilyName`, `SizePoints`, `Bold`, `Italic` constructor params
  - `SizeTwips` computed property (points × 20)
  - `SizeHalfPoints` computed property (points × 2, OOXML native unit)
  - `Default` static field (Calibri 11pt, no bold/italic)
  - `FromHalfPoints()` static factory (converts OOXML half-point sizes to points)
  - `TryResolveTypeface(FontResolver)` delegates to `FontResolver.TryGetTypeface()`
- Created `FontPropertiesTests.cs` with 22 tests:
  - Constructor, property access, unit conversions (twips, half-points)
  - Default values and constants
  - FromHalfPoints conversion (including odd values)
  - Record equality semantics
  - TryResolveTypeface: null guard, known font, unknown font, bold/italic passthrough
- 688 total tests passing, 100% line coverage maintained

## Next Step

Step 2.4.2 — Decorations: Underline, strikethrough, double-strikethrough

## Last Commit

5ac1f44 — Implement step 2.3.8: Integration tests for paragraph metrics

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
