# Current Status

## Last Updated

2026-04-07

## Current Phase

Phase 2: Text Layout

## Current Step

Step 2.4.2 — Decorations: Underline, strikethrough, double-strikethrough — **Complete**

- Created `UnderlineStyle` enum (18 values: None, Single, Double, Thick, Dotted, DottedHeavy, Dash, DashedHeavy, DashLong, DashLongHeavy, DotDash, DashDotHeavy, DotDotDash, DashDotDotHeavy, Wave, WavyDouble, WavyHeavy, Words)
- Created `TextDecoration` readonly record struct:
  - `Underline` (default None), `UnderlineColor` (nullable hex RGB), `Strikethrough`, `DoubleStrikethrough`
  - `None` static field for no-decoration default
  - `HasUnderline`, `HasStrikethrough`, `HasAnyDecoration` computed properties
- Created `UnderlineStyleTests.cs` (20 tests) and `TextDecorationTests.cs` (37 tests)
- 745 total tests passing, 100% line coverage maintained

## Next Step

Step 2.4.3 — Color: Foreground color, resolved from theme color + tint/shade or explicit RGB

## Last Commit

c7f6246 — Implement step 2.4.1: Font properties

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
