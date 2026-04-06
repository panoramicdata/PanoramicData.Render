# Current Status

## Last Updated

2026-04-05

## Current Phase

Phase 1: Foundation

## Current Step

Step 1.4.2 (TrueType Collection face enumeration) — **Complete**

- Added `IFontMetadataReader` abstraction and `SkiaFontMetadataReader` implementation:
  - Uses SkiaSharp to read family names from font files
  - Enumerates TTC faces by index and collects discovered families
  - Handles malformed/unreadable font data safely
- Updated `FontResolver` to:
  - read family metadata through `IFontMetadataReader`
  - index all TTC-discovered family names to the same `.ttc` path
  - fall back to filename when metadata is unavailable
  - support test-time metadata reader injection via internal constructor
- Expanded tests:
  - `FontResolverTests` for TTC multi-family indexing, metadata-empty fallback, and constructor guard
  - `SkiaFontMetadataReaderTests` for TTC enumeration flow, deduplication, null/whitespace/missing path handling, and default-reader success/failure paths
- 229 total tests passing
- 100% line coverage overall maintained

## Next Step

Step 1.4.3 — Implement font substitution mapping (`RenderOptions.FontSubstitutions`)

## Last Commit

f8a424e — Implement step 1.4.1: scan font directories and build family index

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
