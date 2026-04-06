# Current Status

## Last Updated

2026-04-05

## Current Phase

Phase 1: Foundation

## Current Step

Step 1.3.8 (table style resolution) — **Complete**

- `ResolvedTableStyle` model: resolved table/table-row/table-cell/paragraph/run style fragments and applied conditionals
- `TableStyleResolver`: resolves table style by ID and applies conditional formatting fragments in caller-provided order
  - Resolves table style from styles part (`w:style` type table)
  - Uses base style fragments (`w:tblPr`, `w:pPr`, `w:rPr`)
  - Applies conditional band fragments from `w:tblStylePr` (first row, banded rows, etc.)
  - Handles missing styles and unknown conditionals safely
- Added 10 table style resolver tests covering base resolution, conditional application, ordering behavior, clone behavior, and guard paths
- 188 total tests passing
- 100% line coverage overall; new table style resolver code covered

## Next Step

Step 1.3.9 — Compute effective formatting for any given paragraph + run (full cascade)

## Last Commit

Pending — step 1.3.8 implementation

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
