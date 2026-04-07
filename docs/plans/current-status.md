# Current Status

## Last Updated

2026-04-07

## Current Phase

Phase 2: Text Layout

## Current Step

Step 2.3.4 — Spacing: Space before/after paragraph, line spacing — **Complete**

- Created `LineSpacingRule` enum with Auto, Exact, AtLeast values
- Created `ParagraphSpacing` readonly record struct with:
  - SpaceBefore, SpaceAfter, LineSpacingTwips (all floats in twips)
  - LineRule (nullable LineSpacingRule, defaults to Auto)
  - `EffectiveLineRule` property (null → Auto)
  - `GetLineSpacingMultiplier()` — baseline 240 twips = 1.0×
  - `ComputeLineHeight(naturalLineHeight)` — applies Auto/Exact/AtLeast rules
  - `ComputeParagraphHeight(lineCount, naturalLineHeight)` — total height including before/after
- Added 31 tests covering:
  - Defaults/None, EffectiveLineRule, GetLineSpacingMultiplier (6 values)
  - ComputeLineHeight for Auto (4), Exact (4), AtLeast (4)
  - ComputeParagraphHeight (6 scenarios), record equality (2)
- 547 total tests passing, 100% line coverage maintained

## Next Step

Step 2.3.5 — Tab stops: Left, center, right, decimal, bar tab stops; leader characters (dot, hyphen, underscore)

## Last Commit

af9cc44 — Implement step 2.3.3: Indentation

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
