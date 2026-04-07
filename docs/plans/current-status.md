# Current Status

## Last Updated

2026-04-07

## Current Phase

Phase 2: Text Layout

## Current Step

Step 2.2.6 — Compute line break positions for a paragraph given a target line width — **Complete**

- Created `ParagraphLineBreaker` class that orchestrates the full pipeline:
  - Takes `IReadOnlyList<ParsedRun>`, typeface, font size, and line width in twips
  - Uses `TextRunToItemMapper` to convert all run elements to Knuth-Plass items
  - Appends standard paragraph-finishing sequence: finishing glue (infinite stretch) + forced break penalty
  - Calls `KnuthPlassAlgorithm.FindBreaks` to compute optimal line breaks
  - Provides both `ComputeLineBreaks` (lines only) and `ComputeLineBreaksWithItems` (lines + items tuple)
- Supports optional `HyphenationDictionary` passed through to the mapper
- Added 17 new tests covering:
  - Guard tests (null runs, null typeface, zero font size, zero line width)
  - Empty input (no runs, empty text)
  - Single line (short text fits on one line)
  - Multiple lines (long text, contiguous indices)
  - Forced breaks (line breaks produce multiple lines)
  - Multiple runs (combined correctly)
  - Adjustment ratio (last line not stretched)
  - With hyphenation (can break at hyphenation points)
  - Items accessor (returns both lines and items)
  - Item index validity (all indices reference valid items)
  - Paragraph terminator (last item is forced break penalty)
  - Finishing glue (infinite stretch, zero width/shrink)
- 441 total tests passing, 100% line coverage maintained

## Next Step

Step 2.2.7 — Unit tests: verify break positions against hand-computed expected results

## Last Commit

35d00e0 — Implement step 2.2.5: TeX hyphenation patterns for automatic hyphenation

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
