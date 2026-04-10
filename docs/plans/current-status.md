# Current Status

## Last Updated

2026-04-10

## Current Phase

Phase 6: Output Drivers — **IN PROGRESS**

## Current Step

Steps 6.4.3–6.4.5 — PDF page flow and rendering primitives — **COMPLETE**

Completed core multi-page PDF rendering behavior:
- Added explicit multi-page flow with `BeginPage`/`EndPage` handling in `PdfRenderTarget`
- Updated `PdfPageRenderer` to emit all layout pages into one PDF document
- Confirmed text rendering and image drawing through PDF output tests
- Added/expanded PDF test coverage: `PdfPageRendererTests` and `PdfRenderTargetTests`

1716 tests passing (1713 → 1716, +3 new PDF tests).

## Next Step

Steps 6.4.6–6.4.8 — PDF metadata and final validation

## Last Commit

Implement steps 6.4.1-6.4.2: Add PdfRenderTarget foundation (commit 21431e9)

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
- `PdfRenderTarget` now supports multi-page output; metadata emission and stronger PDF structure assertions remain for 6.4.6–6.4.8

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
