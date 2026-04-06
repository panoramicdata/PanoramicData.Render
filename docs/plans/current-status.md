# Current Status

## Last Updated

2026-04-05

## Current Phase

Phase 1: Foundation

## Current Step

Step 1.3.9 (effective formatting computation) — **Complete**

- `EffectiveFormatting` model: merged paragraph/run properties, resolved toggle state, resolved run color, and numbering level
- `EffectiveFormattingResolver`: computes effective formatting by walking full cascade order
  - Applies doc defaults
  - Applies table style fragments
  - Applies paragraph style chain (`basedOn`, root-to-leaf)
  - Applies character style chain (`basedOn`, root-to-leaf)
  - Applies toggle semantics through each run-stage layer
  - Applies direct paragraph/run formatting as final override
  - Resolves theme-based run color via `ThemeColorResolver`
- Added 9 effective formatting resolver tests covering precedence, toggle behavior, theme color resolution, numbering pass-through, and guard paths
- 197 total tests passing
- 100% line coverage overall; new effective formatting code covered

## Next Step

Step 1.3.10 — Expand cascade verification to at least 20 focused style-resolution test cases

## Last Commit

Pending — step 1.3.9 implementation

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
