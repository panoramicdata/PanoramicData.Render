# Current Status

## Last Updated

2026-04-09

## Current Phase

Phase 5: Graphics & Objects — **IN PROGRESS**

## Current Step

Step 5.4.8 — Unit tests for top 10 preset geometries — **COMPLETE**

Implemented grouped shape support and geometry render tests:
- added `GroupedShapeItem` model
- added `DrawingGroupRunElement` model
- added `GroupShapeParser` to recursively parse `wpg:wgp` children (preset and custom shapes)
- wired group detection into `RunElementParser` for both inline and anchor drawings (checked before preset/custom/image)
- added `DrawingGroupRunElementTests` covering flat groups, offsets, child kinds, anchor groups, and nested groups
- added `PresetGeometryRenderMetadataTests` verifying full metadata round-trip for top 10 presets

1668 tests passing.

## Next Step

Step 5.5.1 — Detect chart elements

## Last Commit

Step 5.4.8: Geometry render tests for top 10 preset shapes

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
