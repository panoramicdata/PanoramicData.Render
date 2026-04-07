# Current Status

## Last Updated

2026-04-07

## Current Phase

Phase 2: Text Layout

## Current Step

Step 2.3.8 — Unit tests: verify paragraph metrics for each formatting type — **Complete**

- Created `ParagraphMetricsIntegrationTests.cs` with 17 cross-cutting integration tests:
  - Spacing + Indentation combined (height + offset verification)
  - Center-aligned + Indentation + Spacing (all three dimensions)
  - Right-aligned + Hanging indent (multi-line offset verification)
  - Exact/AtLeast spacing with varied natural line heights
  - Borders contributing to total paragraph height geometry
  - Left/Right borders narrowing effective content width
  - Shading does not affect content positioning
  - Tab stops resolving with indentation offsets
  - Right/Decimal tab alignment with content widths
  - Full 3-line justified paragraph with all formatting combined
  - Single-line paragraph is both first and last
  - Empty paragraph edge case
  - Default tab stops with indentation
- Section 2.3 (Paragraph Formatting) is now COMPLETE (all 8 steps)
- 660 total tests passing, 100% line coverage maintained

## Next Step

Step 2.4.1 — Font properties: Family, size, bold, italic — select correct SKTypeface

## Last Commit

d81aafd — Implement step 2.3.7: Borders and shading

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
