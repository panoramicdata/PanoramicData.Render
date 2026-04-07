# Current Status

## Last Updated

2026-04-07

## Current Phase

Phase 2: Text Layout

## Current Step

Step 2.3.3 — Indentation: First-line indent, hanging indent, left margin, right margin — **Complete**

- Created `ParagraphIndentation` readonly record struct with Left, Right, FirstLine, Hanging (all floats in twips)
  - `GetFirstLineLeftIndent()`: Hanging > 0 → Left; else → Left + FirstLine
  - `GetSubsequentLineLeftIndent()`: Hanging > 0 → Left + Hanging; else → Left
  - `static readonly None` for zero indentation
- Updated `ParagraphAligner.ComputeBoxPositions` with `indentation` and `isFirstLine` parameters
  - Computes effective line width after subtracting left and right indentation (clamped to 1f minimum)
  - Alignment offset computed within indented area; total X = leftIndent + alignmentOffset
- Updated `ComputeParagraphBoxPositions` to accept indentation and auto-detect `isFirstLine = i == 0`
- Added 12 tests for `ParagraphIndentation` record struct
- Added 10 integration tests for indentation in `ParagraphAlignerTests`:
  - Left indent, right indent with center/right alignment, first-line indent, hanging indent
  - Both left+right with center, paragraph-level with hanging/first-line, extreme indentation clamping
- 516 total tests passing, 100% line coverage maintained

## Next Step

Step 2.3.4 — Spacing: Space before/after paragraph (in twips), line spacing (single, 1.5, double, exact, at-least, multiple)

## Last Commit

fb8acda — Implement step 2.3.2: Last-line justification

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
