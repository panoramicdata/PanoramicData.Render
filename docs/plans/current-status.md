# Current Status

## Last Updated

2026-04-09

## Current Phase

Phase 4: Tables

## Current Step

Step 4.8.1 — Parse cell shading — **COMPLETE**

Added table-cell shading parsing to the `TableParser`/`TableCellElement` model surface:
- parsed `w:shd` fill color, pattern color, and pattern into the existing `ParagraphShading` value object
- normalized `auto`/`nil` shading colors to unspecified values
- added parser tests for simple fill shading, patterned shading, and auto-color handling

1487 total tests passing in Release.

## Next Step

Step 4.8.2 — Render cell backgrounds before cell content

## Last Commit

Step 4.7.3: Add nested table layout tests

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
