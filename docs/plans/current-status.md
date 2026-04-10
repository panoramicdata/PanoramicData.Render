# Current Status

## Last Updated

2026-04-10

## Current Phase

Phase 7: Advanced Features — **IN PROGRESS**

## Current Step

Steps 7.1.1–7.1.6 — field parsing and computed values — **COMPLETE**

Completed initial field support:
- Added parsing for complex fields (`w:fldChar` begin/separate/end + field code runs) and `w:fldSimple`
- Rendered field result text while skipping field code instructions
- Added computed value handling for `PAGE`, `NUMPAGES`, `DATE`, and `TIME`
- Added cached result rendering for `TOC`
- Wired consistent total-page count and render timestamp into SVG/PDF page emitters
- Added unit tests for complex and simple field scenarios, including PAGE/NUMPAGES/DATE/TOC

1727 tests passing (1722 → 1727, +5 field tests).

## Next Step

Steps 7.1.7–7.1.10 — field hyperlinks/cross-references and remaining tests

## Last Commit

Implement steps 6.5.1-6.5.4: Output option behaviors and tests (commit 531f4cc)

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
