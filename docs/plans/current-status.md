# Current Status

## Last Updated

2026-04-11

## Current Phase

Phase 7: Advanced Features — **IN PROGRESS**

## Current Step

Step 7.5.2 — Parse hyperlinks — **COMPLETE**

Added `HyperlinkUri` property to `ParsedRun`. Updated `RunElementParser.ParseParagraphRuns()`
to iterate all paragraph children (not just `Run`), handling `w:hyperlink` wrappers by resolving
external URIs via `HyperlinkRelationship` and internal bookmarks via `w:anchor` attribute.
Added `ResolveHyperlinkUri()` helper. Added 11 tests in `HyperlinkParsingTests.cs` covering
external/internal links, mixed hyperlinks, edge cases, and DOCX round-trips.
Added 3 `TestDocxBuilder` helpers for hyperlink test documents.

1820 tests passing (1809 → 1820, +11 hyperlink parsing tests).

## Next Step

Step 7.5.3 — Emit hyperlinks in SVG (`<a>`) and PDF (link annotations)

## Last Commit

Implement step 7.5.1: Parse bookmark start/end elements (commit e860023)

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
