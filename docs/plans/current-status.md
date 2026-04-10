# Current Status

## Last Updated

2026-04-10

## Current Phase

Phase 7: Advanced Features — **IN PROGRESS**

## Current Step

Steps 7.2.1–7.2.8 — multi-level lists — **COMPLETE**

Completed multi-level list support milestone:
- Added restart metadata (`RestartAfterLevel`) to resolved numbering level styles
- Extended `NumberingStyleResolver` to map `w:lvlRestart`
- Implemented `ListNumberingFormatter` for decimal/alpha/roman/bullet label text and `%n` pattern expansion
- Implemented `ListNumberingState` for per-instance counters and restart-aware sequence tracking
- Integrated list label emission into `RenderCommandEmitter` with hanging-indent style positioning
- Added support for bullet label fonts via resolved/configured numbering style font family
- Added unit tests for numbering sequences, restart behavior, label positioning, and bullet font rendering

1748 tests passing (1743 → 1748, +5 list integration tests).

## Next Step

Steps 7.3.1–7.3.7 — text boxes parsing, layout, and positioning

## Last Commit

Implement steps 7.2.5-7.2.8: List render integration and positioning (commit 9a4d948)

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
