# Current Status

## Last Updated

2026-04-05

## Current Phase

Phase 1: Foundation

## Current Step

Step 1.3.6 (toggle property logic) — **Complete**

- `ToggleInstruction` enum: `None`, `Toggle`, `SetFalse`
- `ToggleProperties` model: parsed instruction set for all run toggle properties
- `ToggleState` model: resolved boolean state for all run toggle properties
- `TogglePropertyLogic`: parse and apply toggle semantics
  - `Parse(StyleRunProperties?)`: maps bold/italic/caps/smallCaps/strike/double-strike/vanish/emboss/imprint/outline/shadow
  - `Apply(bool, ToggleInstruction)`: applies per-property toggle/force-false/no-op behavior
  - `Apply(ToggleState, ToggleProperties)`: applies full toggle set to inherited state
- Added 10 toggle logic tests covering parse mappings, instruction semantics, full-state application, and null guards
- 166 total tests passing
- 100% line coverage overall; new toggle logic code covered

## Next Step

Step 1.3.7 — Implement numbering style resolution: abstract numbering -> numbering instance -> level overrides

## Last Commit

Pending — step 1.3.6 implementation

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
