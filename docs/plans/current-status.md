# Current Status

## Last Updated

2026-04-11

## Current Phase

Phase 7: Advanced Features — **IN PROGRESS**

## Current Step

Step 7.8.4 — RTL table layout — **COMPLETE**

Added `IsBiDi` property to `TableElement` (parsed from `w:bidiVisual` in TableParser).
Mirrored column offsets in `ComputeColumnOffsets(columnWidths, isBiDi)`: each column offset becomes `totalWidth - ltrOffset - columnWidth`.
Fixed table width calculation to use `SumColumnWidths` instead of relying on offsets (which are mirrored for BiDi).
Added 5 tests (3 column offset BiDi + 2 parser BiDi).

1942 tests passing (1937 → 1942).

## Next Step

Step 7.8.5 — Integrate with HarfBuzz

## Last Commit

Implement step 7.8.3: Mirror paragraph layout for RTL (commit 6b16101)
- Renamed `HeaderFooterType` to `HeaderFooterKind` to avoid collision with `DocumentFormat.OpenXml.Wordprocessing.HeaderFooterType`
- OpenXML `EnumValue<T>` types cannot be used in C# switch patterns; use `if` chains with `==` instead
- Using TDD + spec-driven development from this point forward
- `FontEmbedder` uses caching to avoid repeated disk I/O for the same font families
- When `RenderOptions.FontDirectories` is empty, font embedding silently skips (no exception) — allows graceful degradation
- TTF font embedding chosen as pragmatic MVP; WOFF2 deferred pending suitable library (Google's woff2 C++ requires P/Invoke, SixLabors.Fonts has no export API)
- `PdfRenderTarget` writes metadata when provided, but automatic extraction from DOCX core properties is pending full top-level render pipeline wiring

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
- Font embedding via TTF data URIs (pragmatic MVP; WOFF2 upgrade deferred pending library availability)
