# Current Status

## Last Updated

2026-04-05

## Current Phase

Phase 1: Foundation

## Current Step

Step 1.2.5 (Media extraction) — **Complete**

- `ImageData` record: raw bytes + MIME content type
- `MediaStore`: resolves relationship IDs to image data with caching
  - `TryGetImage(relId, out ImageData?)`: retrieves and caches image bytes
  - `GetImagePartRelationshipIds()`: lists all image part IDs
- `DocxDocument.MainDocumentPart` exposed for MediaStore access
- 9 media tests + 20 run + 14 block + 26 section + 10 DocxDocument = 79 total
- 100% line coverage

## Next Step

Step 1.2.6 — Parse header and footer parts

## Last Commit

Pending — step 1.2.5 implementation

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
