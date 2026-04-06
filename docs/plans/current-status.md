# Current Status

## Last Updated

2026-04-05

## Current Phase

Phase 1: Foundation

## Current Step

Step 1.4.1 (font directory scanning/indexing) — **Complete**

- `FontResolver` implementation:
  - Scans configured directories recursively
  - Indexes supported font extensions: `.ttf`, `.otf`, `.ttc`
  - Builds case-insensitive family-name -> font-file-path index
  - Ignores missing/invalid directories and unsupported file types
  - Exposes `TryGetFontPath` for case-insensitive family lookup
- Added 9 `FontResolverTests` covering:
  - null/empty/missing directory behavior
  - recursive scanning and extension filtering
  - case-insensitive extension and family matching
  - duplicate family handling and guard paths
- 217 total tests passing
- 100% line coverage overall maintained

## Next Step

Step 1.4.2 — Handle TrueType Collections (`.ttc`): enumerate faces within a collection

## Last Commit

Pending — step 1.4.1 implementation

## Implementation Notes

- `DocxDocument` is internal; test project accesses it via `InternalsVisibleTo`
- `TestDocxBuilder` helper creates minimal and full DOCX files in-memory for tests
- `DocxDocument.Load` disposes the underlying `WordprocessingDocument` on constructor failure (no resource leak)
- Renamed `HeaderFooterType` to `HeaderFooterKind` to avoid collision with `DocumentFormat.OpenXml.Wordprocessing.HeaderFooterType`
- OpenXML `EnumValue<T>` types cannot be used in C# switch patterns; use `if` chains with `==` instead
- Using TDD + spec-driven development from this point forward

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
