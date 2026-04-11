# Current Status

## Last Updated

2026-04-11

## Current Phase

Phase 7: Advanced Features — **IN PROGRESS**

## Current Step

Step 7.7.2 — Handle decimal tab stops — **COMPLETE**

Major refactor of `AppendSegmentsFromRun` to process run `ChildElements` in document order,
capturing `TabChar` elements as tab markers (`IsTab=true`). Extracted `RouteTextToSegment`
helper for field-context routing. Rewrote the emit loop from `foreach` to indexed `for` loop
with tab stop resolution via `TabStopProfile.ResolveNextTabStop` — decimal tabs look ahead
via `GetTextAfterTab` to find the decimal point and align using `TabStopResolver.ComputeContentStart`.
Right/Center tabs also look ahead for content width. Left/Bar tabs position directly.

Fixed `AppendTextSegment` merging bug: it was merging text into the preceding tab segment
when they shared the same font — added `!segments[^1].IsTab` guard.

1909 tests passing (1904 → 1909, +5 tab positioning tests: decimal align, decimal no-dot,
right, center, left).

## Next Step

Step 7.7.3 — Handle leader characters: dot leader, hyphen leader, underscore leader, heavy leader

## Last Commit

Implement step 7.7.1: Bar tab stops (commit 745576a)
- When no stretch available (ratio > tolerance), accepted with 10K extra demerits
- Application Control policy may block Debug DLLs on this machine; use Release for coverage
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
