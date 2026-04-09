# Current Status

## Last Updated

2026-04-10

## Current Phase

Phase 5: Graphics & Objects — **IN PROGRESS**

## Current Step

Steps 5.5.1–5.5.3, 5.6.1–5.6.3, 5.7.1–5.7.3 — Charts, SmartArt, OLE best-effort — **COMPLETE**

Implemented best-effort detection for Charts, SmartArt and OLE objects:
- added `ChartRunElement` model (RelationshipId, WidthEmu, HeightEmu, FallbackImageRelationshipId, HasFallbackImage)
- added `SmartArtRunElement` model (RelationshipId, WidthEmu, HeightEmu, HasFallback)
- added `OleObjectRunElement` model (RelationshipId, WidthEmu, HeightEmu, PreviewImageRelationshipId, HasPreviewImage)
- wired chart detection (`c:chart` local-name) into `RunElementParser` inline/anchor branches
- wired SmartArt detection (`dgm:relIds` local-name) into `RunElementParser` inline/anchor branches
- wired OLE detection (`EmbeddedObject` case) into `RunElementParser` main switch
- added `ChartRunElementTests`, `SmartArtRunElementTests`, `OleObjectRunElementTests`

1687 tests passing.

## Next Step

Phase 5 complete — Phase 6 (Output Drivers)

## Last Commit

Implement steps 5.5–5.7: chart, SmartArt, OLE best-effort detection

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
