# Current Status

## Last Updated

2026-04-08

## Current Phase

Phase 4: Tables

## Current Step

Step 4.5.3 — Handle insideH and insideV borders — **COMPLETE**

Extended `TableBorderResolver.ResolveCellEdge` with optional position flags
(`isFirstRow`, `isLastRow`, `isFirstColumn`, `isLastColumn`, all defaulting to `true`).
When a cell edge is an inner edge (not on the table boundary), the resolver
falls back to the table's `InsideHorizontal` or `InsideVertical` border instead
of the outer border. Existing tests required no changes (defaults preserve prior behavior).

Added 9 new `TableBorderResolverTests` covering insideH/V selection, outer vs. inner
boundary disambiguation, cell precedence over insideH, and no-border-defined cases.

1447 total tests passing. `TableBorderResolver` line coverage: 100%.

## Next Step

Step 4.5.4 — Handle border spacing (distance between border and cell content)

## Last Commit

Step 4.5.3: Handle insideH and insideV borders

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
