# Current Status

## Last Updated

2026-04-07

## Current Phase

Phase 2: Text Layout

## Current Step

Steps 2.3.5 + 2.3.6 — Tab stops and default tab stops — **Complete**

- Created `TabStopType` enum: Left, Center, Right, Decimal, Bar
- Created `TabStopLeader` enum: None, Dot, Hyphen, Heavy, MiddleDot, Underscore
- Created `TabStop` readonly record struct: PositionTwips, Type, Leader
- Created `TabStopProfile` readonly record struct:
  - ExplicitStops (sorted by position) + DefaultIntervalTwips (default 720 twips = 0.5 inch)
  - `ResolveNextTabStop(currentX)` — finds next explicit stop or generates from default interval
  - Handles disabled default tabs (zero/negative interval) with minimal advance
- Created `TabStopResolver` static class:
  - `ComputeContentStart(tabStop, contentWidthAfterTab, widthBeforeDecimal)` — computes X position per tab type
  - Left/Bar: content starts at position; Center: centered on position; Right: ends at position; Decimal: decimal point at position
- Added 22 TabStopTests covering: record defaults, equality, explicit stop resolution, default generation, edge cases
- Added 17 TabStopResolverTests covering: all 5 types, clamping, zero-width, leaders, unknown type fallback
- 586 total tests passing, 100% line coverage maintained

## Next Step

Step 2.3.7 — Borders and shading: Paragraph borders (top, bottom, left, right, between), paragraph background color

## Last Commit

bdb1171 — Implement step 2.3.4: Spacing

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
