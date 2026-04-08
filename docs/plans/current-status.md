# Current Status

## Last Updated

2026-04-07

## Current Phase

Phase 3: Page Layout

## Current Step

Step 3.3.1 — Resolve header/footer per page — **COMPLETE**

Created `HeaderFooterResolver` with `ResolveHeader`/`ResolveFooter` methods implementing
OOXML rules: titlePage→First, evenAndOddHeaders→Even, fallback→Default.
Added `TitlePage` property to SectionInfo and parsing of `w:titlePg` in SectionInfoParser.
17 resolver tests + 3 TitlePage parsing tests.

1048 total tests passing, 100% line coverage.

## Next Step

Step 3.3.2 — Lay out header content using the text layout engine

## Last Commit

Step 3.3.1: Resolve header/footer per page

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
