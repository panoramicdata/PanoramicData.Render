# Current Status

## Last Updated

2026-04-07

## Current Phase

Phase 2: Text Layout

## Current Step

Phase 2 (Text Layout) — **COMPLETE**

All four sections completed:
- Section 2.1: Measurement Engine (steps 2.1.1–2.1.5) ✅
- Section 2.2: Knuth-Plass Line Breaking (steps 2.2.1–2.2.7) ✅
- Section 2.3: Paragraph Formatting (steps 2.3.1–2.3.8) ✅
- Section 2.4: Character Formatting (steps 2.4.1–2.4.9) ✅

902 total tests passing, 100% line coverage maintained throughout.

## Next Step

Phase 3 — Page Layout — Step 3.1.1: Implement PageBuilder

## Last Commit

a4ca11f — Implement step 2.4.9: Integration tests for character formatting — completes Section 2.4

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
