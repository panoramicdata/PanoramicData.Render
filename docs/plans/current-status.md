# Current Status

## Last Updated

2026-04-11

## Current Phase

Phase 8: Quality & Performance — **IN PROGRESS**

## Current Step

Steps 8.3, 8.6, 8.7 — Performance, thread safety, documentation — **PARTIAL**

Performance:
- Font cache already optimized (ConcurrentDictionary, shared SKTypeface instances)
- SVG already uses StringBuilder throughout
- Style cache and benchmarks pending

Documentation:
- All public API members have XML docs (0 warnings)
- Created `docs/supported-features.md` — comprehensive feature matrix
- Created `docs/known-limitations.md` — documenting all known limits
- NuGet metadata verified (package ID, license, tags, readme, icon)

Thread safety:
- FontResolver._typefaceCache is ConcurrentDictionary
- 100 concurrent renders verified via stress test
- 5 thread safety tests passing

2013 tests passing.

## Next Step

Remaining Phase 8 items:
- 8.1.2, 8.1.6, 8.1.7 — CI/visual regression infrastructure (requires CI setup)
- 8.2 — Test document corpus (requires manual DOCX creation)
- 8.3.1, 8.3.3, 8.3.5, 8.3.6, 8.3.7 — Profiling and benchmarks
- 8.4 — Memory optimization and profiling
- 8.5.6 — Torture test corpus
- 8.7.1, 8.7.6 — README finalization and v1.0.0 tag

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
