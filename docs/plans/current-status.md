# Current Status

## Last Updated

2026-04-11

## Current Phase

Phase 7: Advanced Features — **IN PROGRESS**

## Current Step

Step 7.8.3 — Mirror paragraph layout for RTL — **COMPLETE**

Added `ParagraphAlignment` property to `ParagraphBlock` (parsed from `w:jc` via `MapJustification`).
BiDi paragraphs default to right-aligned when no explicit alignment is set.
Added alignment offset computation in emit loop (right and center alignment).
RTL list labels are positioned from the right margin.
Added 6 tests (3 emitter alignment + 3 parser alignment).

1937 tests passing (1931 → 1937).

## Next Step

Step 7.8.4 — Handle RTL table layout

## Last Commit

Implement step 7.8.2: Unicode BiDi algorithm (commit 127cf47)
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
