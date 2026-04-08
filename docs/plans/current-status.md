# Current Status

## Last Updated

2026-04-07

## Current Phase

Phase 3: Page Layout

## Current Step

Step 3.4.3 — Reserve footnote space — **COMPLETE**

Added `footnoteHeight` parameter to `Paginate`, `PaginateStartingAt`, and
`ComputeAvailableContentHeight`. Added `ComputeFootnoteTop` method for positioning
footnotes above the footer. 8 new tests covering footnote space reservation,
combined header/footer/footnote reductions, positioning, and pagination impact.

1127 total tests passing, 100% line coverage.

## Next Step

Step 3.4.4 — Handle footnotes that exceed remaining page space: continue on next page

## Last Commit

Step 3.4.3: Reserve footnote space

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
