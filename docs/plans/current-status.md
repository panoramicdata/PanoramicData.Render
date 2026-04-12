# Current Status

## Last Updated

2026-04-11

## Current Phase

Phase 8: Quality & Performance — **IN PROGRESS**

## Current Step

Steps 8.3.3 + 8.3.5 + 8.4.4 + 8.4.5 — Caching, image handling, memory verification — **COMPLETE**

- Style resolution chains already pre-computed (O(1) lookup)
- Image caching already implemented in MediaStore (no re-decoding)
- Memory leak test: 200 sequential renders, growth < 50MB after GC
- Stream disposal: verified no reference leaks via WeakReference tracking

2060 tests passing.

## Next Step

Remaining Phase 8 items (all require infrastructure or manual work):
- 8.1.2 — Reference PNGs (requires Word rendering)
- 8.1.6 — CI integration
- 8.1.7 — HTML visual diff report
- 8.2.1-8.2.3 — Test document corpus (manual DOCX creation)
- 8.3.1 — Profiling (requires profiler tool)
- 8.3.6 — BenchmarkDotNet (requires adding benchmark project)
- 8.4.1-8.4.3 — Memory profiling and streaming optimization
- 8.7.6 — Tag and publish v1.0.0

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
