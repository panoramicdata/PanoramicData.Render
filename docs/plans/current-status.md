# Current Status

## Last Updated

2026-04-09

## Current Phase

Phase 5: Graphics & Objects — **IN PROGRESS**

## Current Step

Step 5.1.4 — Position inline images within text flow for line breaking — **COMPLETE**

Completed inline image groundwork and line-flow integration for Phase 5.1.1-5.1.4:
- validated existing `w:drawing`/`wp:inline` parsing into `InlineImageRunElement`
- expanded media extraction tests across JPEG, PNG, GIF, BMP, TIFF, WMF, and EMF part types
- added `TwipConverter.EmusToTwips(long)` for image extent sizing conversion
- mapped `InlineImageRunElement` to fixed-width `KnuthPlassBox` entries so inline images participate in paragraph line breaking as large inline glyphs

1508 total tests passing in Release.

## Next Step

Step 5.1.5 — Handle image cropping (`a:srcRect`)

## Last Commit

Step 5.1.4: Inline image sizing and line-flow mapping

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
