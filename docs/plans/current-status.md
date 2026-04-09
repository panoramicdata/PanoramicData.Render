# Current Status

## Last Updated

2026-04-09

## Current Phase

Phase 5: Graphics & Objects — **IN PROGRESS**

## Current Step

Step 5.2.7 — Unit tests: verify floating image positions relative to different anchor types — **COMPLETE**

Completed floating-image position test expansion for Phase 5.2.7:
- added `AnchorPositionResolverTests` coverage for page, margin, paragraph, column, and character anchor references
- verified alignment-aware placement and absolute offset application in twips
- verified margin-edge references (`rightMargin` / `bottomMargin`) resolve to expected absolute coordinates
- retained null-guard coverage for resolver inputs

1526 tests passing via the test runner in this environment.

## Next Step

Step 5.3.1 — Square wrapping: text flows around image bounding rectangle with configurable distances

## Last Commit

Step 5.2.7: Verify floating image anchor positioning tests

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
