# Current Status

## Last Updated

2026-04-11

## Current Phase

Phase 7: Advanced Features — **IN PROGRESS**

## Current Step

Step 7.5.4 — Emit internal bookmarks in PDF as named destinations — **COMPLETE**

Added `SetNamedDestination(string name, float xTwips, float yTwips)` to `IRenderTarget`.
PdfRenderTarget uses `DrawNamedDestinationAnnotation` to place named destinations at bookmark
positions. SvgRenderTarget emits `<a id="name">` anchor elements. RenderCommandEmitter iterates
`ParagraphBlock.BookmarkStarts` before drawing text and calls `SetNamedDestination` for each.
Added 7 tests: 3 emitter (single/multiple/no bookmarks), 2 PDF (valid+null), 2 SVG (valid+null).

1832 tests passing (1825 → 1832, +7 named destination tests).

## Next Step

Step 7.5.5 — Unit tests: verify hyperlinks are emitted in both SVG and PDF output

## Last Commit

Implement step 7.5.3: Emit hyperlinks in SVG and PDF (commit abdf4d6)

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
