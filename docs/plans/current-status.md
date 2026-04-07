# Current Status

## Last Updated

2026-04-07

## Current Phase

Phase 2: Text Layout

## Current Step

Step 2.4.9 — Unit tests: verify each formatting property produces correct render instructions — **Complete**

- Created `CharacterFormattingIntegrationTests.cs` with 12 cross-cutting integration tests:
  - Superscript reduces font + raises baseline
  - Subscript reduces font + lowers baseline
  - SmallCaps with font size differentiation (lowercase smaller, uppercase full)
  - Red text on yellow highlight
  - Auto color on no highlight
  - Bold+Italic with wavy underline + strikethrough
  - Expanded spacing with small caps
  - Vanish hides run regardless of other formatting
  - Full formatting: all properties combined
  - Font size pipeline: half-points → points → twips
  - Default formatting produces minimal output
  - Double-strikethrough vs single-strikethrough semantics
- Section 2.4 (Character Formatting) is now COMPLETE (all 9 steps)
- 902 total tests passing, 100% line coverage maintained

## Next Step

Phase 2 Section 2.5 (if it exists) or Phase 3

## Last Commit

970b880 — Implement step 2.4.8: Vanish (hidden text)

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
