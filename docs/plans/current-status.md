# Current Status

## Last Updated

2026-04-08

## Current Phase

Phase 4: Tables

## Current Step

Step 4.5.2 — Border conflict resolution — **COMPLETE**

Added row-level border storage to `TableRowElement` and row-border parsing from
table property exceptions in `TableParser` (`w:tblPrEx`/`w:tblBorders`).
Implemented `TableBorderResolver` to resolve edge borders using precedence:
cell > row > table.

Added focused tests:
- `TableBorderResolverTests` for precedence, null guards, and unsupported edge behavior
- `TableParserTests` coverage for row-level border parsing

1438 total tests passing. Coverage verification via test runner reports changed files at 100%.

## Next Step

Step 4.5.3 — Handle `insideH` and `insideV` borders (internal grid lines)

## Last Commit

Step 4.5.2: Resolve border precedence

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
