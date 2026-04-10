# Current Status

## Last Updated

2026-04-10

## Current Phase

Phase 6: Output Drivers — **IN PROGRESS**

## Current Step

Steps 6.3.1–6.3.6 — SVG font embedding — **COMPLETE**

Completed full SVG font embedding pipeline:
- Implemented `FontEmbedder` utility to read TTF/OTF font files from disk and encode as Base64
- Modified `SvgRenderTarget` to track fonts during DrawText() calls
- Emit @font-face CSS blocks in SVG <defs> when RenderOptions.EmbedFonts=true
- Font data embedded as data URIs (TTF format, pragmatic MVP vs WOFF2)
- When EmbedFonts=false (default), fonts referenced by name only
- Comprehensive unit tests: SvgRenderTargetTests (3 tests) + FontEmbedderTests (5 tests)

1710 tests passing (1702 → 1710, +8 new font embedding tests).

## Next Step

Steps 6.4.1–6.4.8 — PDF renderer using SkiaSharp

## Last Commit

Implement steps 6.3.2-6.3.3: Comprehensive font embedding tests (commit 10f8deb)

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
