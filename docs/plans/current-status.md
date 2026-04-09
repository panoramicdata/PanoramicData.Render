# Current Status

## Last Updated

2026-04-09

## Current Phase

Phase 5: Graphics & Objects — **IN PROGRESS**

## Current Step

Step 5.3.6 — Multiple floating objects with overlapping wrap regions — **COMPLETE**

Added multi-region coverage:
- `SquareWrapLayoutEngine`: new test for overlapping exclusion intervals (merged correctly)
- `WrapRegionRegistry`: four new multi-region tests covering two square regions, square+top-bottom
  combination (top-bottom supersedes), overlapping square regions (exclusions merge), and
  GetPrimaryLineWidth with two regions

1570 tests passing.

## Next Step

Step 5.3.7 — Unit tests: verify text wrapping produces correct line widths and positions

## Last Commit

Step 5.3.6: Multi-region wrap tests (overlapping exclusions, cross-type combinations)

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
