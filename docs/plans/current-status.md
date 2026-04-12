# Current Status

## Last Updated

2026-04-12

## Current Phase

Phase 8: Quality & Performance — **IN PROGRESS**

## Current Step

Steps 8.1.2 and 8.2 (expanded minimal corpus + PNG comparison) — **IN PROGRESS**

- `PanoramicData.Render.ReferenceGenerator` now supports:
	- `generate-corpus <output-dir>` for OpenXML-based DOCX corpus generation
	- `render <input-dir> [output-dir]` for Word Interop DOCX → PDF and PDFtoImage PDF → PNG
	- `all <docx-dir> <png-dir>` to run both stages
- Generated corpus assets under `PanoramicData.Render.Test/test-assets/`:
	- 18 DOCX files in `PanoramicData.Render.Test/test-assets/docx/`
	- 19 reference PNG files in `PanoramicData.Render.Test/test-assets/reference/`
- Switched Word automation to late-bound COM to avoid Office 15 assembly binding requirements while keeping the same Word render path.
- Added end-to-end PNG comparison test (`VisualRegressionComparisonTests`) using SSIM and per-document thresholds.
- Latest comparison run (Debug) passed for 15 corpus documents with SSIM range ~0.9604 to ~0.9904.
- Known gaps tracked in test output:
	- `page-break.docx` renders 1 page vs Word reference 3 pages (accepted temporarily)
	- `inline-images.docx` Word export fails during reference generation (no baseline PNG yet)
	- `floating-images.docx` Word export fails during reference generation (no baseline PNG yet)

## Next Step

Complete remaining Phase 8 items:
- 8.1.6 — CI integration for visual regression artifacts
- 8.1.7 — HTML visual diff report
- 8.2.1-8.2.2 — Complete remaining corpus items (inline images, floating images, and integration docs)
- 8.3.1, 8.3.6 — Profiling and BenchmarkDotNet
- 8.4.1-8.4.3 — Memory profiling and streaming/disposal improvements
- 8.7.6 — Tag and publish v1.0.0

## Last Commit

Implement step 8.1.2: Reference Generator console app (commit 43aa963)

2060 tests passing.

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
- Font embedding via TTF data URIs (pragmatic MVP; WOFF2 upgrade deferred pending library availability)
