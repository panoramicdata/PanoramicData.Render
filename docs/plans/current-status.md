# Current Status

## Last Updated

2026-04-11

## Current Phase

Phase 8: Quality & Performance — **IN PROGRESS**

## Current Step

Step 8.7.1 — README finalization and public API facade — **COMPLETE**

Created the `DocxRenderer` public facade:
- `DocxRenderer.cs` — main entry point with `RenderAsync(Stream)` and `Render(Stream)`
- `RenderResult.cs` — result class with `Pages`, `ToPdf()`, `ToPdfAsync()`
- `RenderedPage.cs` — per-page class with `ToSvg()`, `WidthPoints`, `HeightPoints`
- `DocumentLayoutEngine.cs` — body block measurement (DocumentBlock → LayoutBlock)
- `PdfMetadata` made public for use in `RenderResult.ToPdfAsync()` / `ToPdf()`

Updated README.md:
- Quick Start uses the real `DocxRenderer` API
- Added Configuration section documenting all `RenderOptions` properties
- Added links to supported-features and known-limitations docs

28 new tests (DocxRendererTests: 17, DocumentLayoutEngineTests: 10 + TestDocumentBlock)
2041 tests passing.

## Next Step

Remaining Phase 8 items:
- 8.3.3 — Style resolution cache (optimization)
- 8.3.5 — Image handling optimization
- 8.7.6 — Tag and publish v1.0.0
- Items requiring infrastructure/manual work (CI, profiling, DOCX corpus, etc.)

## Last Commit

Implement step 7.9.1: Parse block-level SDTs (commit 513af08)
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
