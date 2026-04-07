# Current Status

## Last Updated

2026-04-06

## Current Phase

Phase 2: Text Layout

## Current Step

Step 2.2.3 — Handle forced breaks — **Complete**

- Added `BreakType` property (nullable `RunBreakType?`) to `KnuthPlassPenalty`
  - Allows downstream consumers (pagination engine) to distinguish line/page/column breaks
  - `null` for non-break penalties (e.g., hyphen penalties)
- Added `MapRunElements(IReadOnlyList<RunElement>, SKTypeface, float)` to `TextRunToItemMapper`
  - Processes `TextRunElement` → delegates to existing `MapTextRun`
  - Processes `BreakRunElement` → forced break penalty with `BreakType` tag
  - Processes `TabRunElement` → glue (delegates to `MapTextRun("\t")`)
  - All three break types (Line, Page, Column) produce `NegativeInfinity` penalty
- Added 11 new tests covering:
  - Guard tests (null elements, non-positive font size)
  - Empty list, single text element, multiple text elements
  - Line break, page break, column break (all verify forced penalty + BreakType)
  - Tab element producing glue
  - Complex sequence: text + break + text
  - Forced break zero width and not flagged
- 384 total tests passing, 100% line coverage maintained

## Next Step

Step 2.2.4 — Handle non-breaking spaces and non-breaking hyphens

## Last Commit

91d57cb — Implement step 2.2.2: map text runs to Knuth-Plass items

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
