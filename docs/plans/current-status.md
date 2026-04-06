# Current Status

## Last Updated

2026-04-06

## Current Phase

Phase 2: Text Layout

## Current Step

Step 2.2.1 — Implement the Knuth-Plass optimal paragraph-breaking algorithm — **Complete**

- Created `KnuthPlassItem.cs`: abstract base + `KnuthPlassBox`, `KnuthPlassGlue`, `KnuthPlassPenalty`
- Created `KnuthPlassLine.cs`: readonly record struct (StartIndex, EndIndex, AdjustmentRatio)
- Created `KnuthPlassAlgorithm.cs`: full Knuth-Plass optimal line-breaking implementation
  - Active node list with cumulative width/stretch/shrink tracking
  - Adjustment ratio computation with stretch and shrink
  - Demerits calculation with fitness class, flagged consecutive break, and overfull penalties
  - Forced break handling with proper chaining across paragraph segments
  - Emergency fallback for infeasible breakpoints
  - Walk-back reconstruction skipping restart anchors
- Created `KnuthPlassTests.cs` with 26 tests covering:
  - Item model (Box/Glue/Penalty properties, defaults)
  - Null/range guards
  - Empty items, single box, two boxes fitting, two boxes overflowing
  - Forced breaks (single, multiple), multiple words, adjustment ratio
  - Penalties (high cost, negative encouragement)
  - Edge cases (only forced breaks, no breakpoints, trailing glue, glue after break)
  - Flagged consecutive breaks, loose fitness class, emergency fallback
- 355 total tests passing, 100% line coverage maintained

## Next Step

Step 2.2.2 — Map text runs to Knuth-Plass items: words → boxes, spaces → glue (with stretch/shrink), hyphens → penalties

## Last Commit

62b6107 — Implement step 2.1.5: verify measurements for known fonts

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
