# Current Status

## Last Updated

2026-04-10

## Current Phase

Phase 6: Output Drivers — **IN PROGRESS**

## Current Step

Step 6.3.1 — TTF font embedding — **COMPLETE**

Implemented TTF font embedding for SVG output:
- Created `FontEmbedder` utility class to read font files from disk and encode as Base64
- Modified `SvgRenderTarget` to accept `RenderOptions` and track fonts used during rendering
- Font tracking occurs in DrawText() calls
- When `RenderOptions.EmbedFonts=true`, @font-face CSS blocks are emitted in SVG <defs>
- Each @font-face references TTF data as Base64-encoded data URIs
- Added 3 unit tests verifying font embedding behavior

1705 tests passing (1702 → 1705, +3 new font embedding tests).

## Next Step

Steps 6.3.2–6.3.6 — SVG font embedding integration + options + tests

## Last Commit

Implement step 6.3.1: TTF font embedding for SVG output (commit 2eb5be1)

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
