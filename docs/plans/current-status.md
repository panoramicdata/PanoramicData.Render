# Current Status

## Last Updated

2026-04-10

## Current Phase

Phase 6: Output Drivers — **IN PROGRESS**

## Current Step

Step 6.2.1 — Implement `SvgRenderTarget` — **COMPLETE**

Implemented SVG render target foundation on top of 6.1:
- added `SvgRenderTarget` implementing `IRenderTarget`
- implemented SVG command mapping for text, line, rect, image, path, clipping, hyperlink region
- implemented `BuildSvg()` with root SVG structure, namespaces, `viewBox`, and `<defs>` support for clip paths
- added `SvgRenderTargetTests` verifying root/viewBox output and key command-to-SVG mappings

1695 tests passing.

## Next Step

Step 6.2.2 — Text rendering as grouped glyph runs in SVG

## Last Commit

Implement step 6.1: render target abstraction and command emission

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
