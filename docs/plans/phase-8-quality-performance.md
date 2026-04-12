# Phase 8: Quality & Performance

**Depends on:** Phase 6 (Output Drivers), Phase 7 (Advanced Features)

## Objective

Build the visual regression test suite, optimize performance and memory usage, harden error tolerance, and prepare for production release.

## Roadmap Note

This phase established the current quality baseline. The remaining unfinished non-SPA work from this phase has been moved to GitHub backlog issues and no longer blocks the active roadmap:

- [Issue #18](https://github.com/panoramicdata/PanoramicData.Render/issues/18) — CI integration for visual regression with artifact upload
- [Issue #19](https://github.com/panoramicdata/PanoramicData.Render/issues/19) — HTML visual diff report
- [Issue #20](https://github.com/panoramicdata/PanoramicData.Render/issues/20) — remaining combined-feature integration corpus documents
- [Issue #21](https://github.com/panoramicdata/PanoramicData.Render/issues/21) — 50-page render profiling
- [Issue #22](https://github.com/panoramicdata/PanoramicData.Render/issues/22) — BenchmarkDotNet performance benchmarks
- [Issue #23](https://github.com/panoramicdata/PanoramicData.Render/issues/23) — memory profiling, image streaming, and page disposal
- [Issue #24](https://github.com/panoramicdata/PanoramicData.Render/issues/24) — v1.0.0 release publication

## Steps

### 8.1 Visual Regression Test Suite

- [x] 8.1.1 — Establish baseline generation workflow: document which Word version produces the reference output (pin to a specific build)
- [x] 8.1.2 — Create `PanoramicData.Render.ReferenceGenerator` console app:
  - [x] 8.1.2.1 — Project scaffold: .NET 10.0-windows console app with `Microsoft.Office.Interop.Word` and `PDFtoImage` dependencies
  - [x] 8.1.2.2 — Implement DOCX → PDF conversion via Word Interop (`Application.Documents.Open` → `Document.ExportAsFixedFormat`)
  - [x] 8.1.2.3 — Implement PDF → PNG conversion via PDFtoImage (`Conversion.SavePng` at 150 DPI)
  - [x] 8.1.2.4 — CLI interface: accept input directory of DOCX files, output directory for PNGs (default: `test-assets/reference/`)
  - [x] 8.1.2.5 — Naming convention: `{docx-stem}_page-{N}.png` (1-indexed)
  - [x] 8.1.2.6 — Add project to solution; update `Directory.Packages.props` with new package versions
- [x] 8.1.3 — Implement SVG-to-PNG rasterization for test comparison (using SkiaSharp or a headless browser)
- [x] 8.1.4 — Implement perceptual image diff: compare rendered PNG against reference PNG using a perceptual diff algorithm (not raw pixel comparison) to avoid false positives from anti-aliasing
- [x] 8.1.5 — Define per-document thresholds: some documents may tolerate more deviation than others
- Deferred backlog: [Issue #18](https://github.com/panoramicdata/PanoramicData.Render/issues/18) — integrate visual regression tests into CI and upload failure artifacts
- Deferred backlog: [Issue #19](https://github.com/panoramicdata/PanoramicData.Render/issues/19) — create an HTML visual diff report showing baseline, actual, and diff side by side

### 8.2 Test Document Corpus

- [x] 8.2.1 — Create minimal test documents (one feature per document):
  - [x] Basic text formatting (`basic-text.docx`)
  - [x] Paragraph alignment and indentation (`paragraph-alignment.docx`, `paragraph-indentation.docx`)
  - [x] Multi-level lists (`multi-level-list.docx`)
  - [x] Simple table (`simple-table.docx`)
  - [x] Merged cells table (`merged-cells-table.docx`)
  - [x] Table styles + conditional formatting (`table-style-first-last.docx`, `table-style-banding.docx`)
    - [x] First/last row and first/last column style conditions
    - [x] Odd/even row and odd/even column banding conditions
    - [x] Built-in Word table style coverage (TableGrid + built-in table look flags)
  - [x] Auto-fit table (`auto-fit-table.docx`)
  - [x] Inline images
  - [x] Floating images with wrapping
  - [x] Headers and footers (`headers-and-footers.docx`)
  - [x] Multi-section with different page sizes (`multi-section.docx`)
  - [x] Footnotes (`footnotes.docx`)
  - [x] Columns (`columns.docx`)
  - [x] Tab stops and leaders (`tab-stops.docx`)
  - [x] Watermark (`watermark.docx`)
  - [x] RTL text (`rtl-text.docx`)
- Deferred backlog: [Issue #20](https://github.com/panoramicdata/PanoramicData.Render/issues/20) — create the remaining combined-feature integration documents:
  - Existing sample complete: `panoramic-data-document-2026.dotx`
  - A realistic business letter
  - A multi-page report with tables and charts
  - A contract with complex numbering and headers
  - A document with mixed content (text, tables, images, footnotes)
- [x] 8.2.3 — Store test documents and reference PNGs in a dedicated `PanoramicData.Render.Test/test-assets/` directory in the repo

### 8.3 Performance Optimization

- Deferred backlog: [Issue #21](https://github.com/panoramicdata/PanoramicData.Render/issues/21) — profile rendering of a 50-page test document and identify hot paths
- [x] 8.3.2 — Optimize font cache: share `SKTypeface` instances across renders; lazy-load fonts
- [x] 8.3.3 — Optimize style resolution: cache computed effective styles per paragraph/run style combination
- [x] 8.3.4 — Optimize SVG string building: use `StringBuilder` or `ArrayBufferWriter<char>` instead of string concatenation
- [x] 8.3.5 — Optimize image handling: avoid unnecessary image decoding/re-encoding; stream images where possible
- Deferred backlog: [Issue #22](https://github.com/panoramicdata/PanoramicData.Render/issues/22) — add BenchmarkDotNet benchmarks for simple, medium, and complex documents
- [x] 8.3.7 — Verify performance targets:
  - 1-page simple document: < 500ms
  - 50-page report: < 10s
  - 500-page document: < 120s

### 8.4 Memory Optimization

- Deferred backlog: [Issue #23](https://github.com/panoramicdata/PanoramicData.Render/issues/23) — profile memory usage for large-image documents
- Deferred backlog: [Issue #23](https://github.com/panoramicdata/PanoramicData.Render/issues/23) — implement image streaming so all images are not retained in memory simultaneously
- Deferred backlog: [Issue #23](https://github.com/panoramicdata/PanoramicData.Render/issues/23) — implement page disposal after rendered output is emitted
- [x] 8.4.4 — Verify peak memory target: < 3× DOCX file size for text-heavy documents
- [x] 8.4.5 — Run long-running render loop to verify no memory leaks (render 1000 documents, monitor RSS)

### 8.5 Error Tolerance

- [x] 8.5.1 — Audit all public entry points: ensure no unhandled exceptions escape to the caller for malformed input
- [x] 8.5.2 — Implement graceful degradation for every unsupported feature: log a warning, render a placeholder or skip
- [x] 8.5.3 — Handle corrupt images: replace with a placeholder rectangle, log a warning
- [x] 8.5.4 — Handle corrupt/incomplete DOCX: render what's available, log errors for missing parts
- [x] 8.5.5 — Handle font loading failures: fall back gracefully, log the failure
- [x] 8.5.6 — Create a "torture test" corpus of malformed documents to verify robustness
- [x] 8.5.7 — Unit tests: verify graceful handling for each error scenario

### 8.6 Thread Safety Verification

- [x] 8.6.1 — Verify `DocxRenderer` is safe for concurrent use: render multiple documents in parallel
- [x] 8.6.2 — Verify font cache thread safety under contention
- [x] 8.6.3 — Verify `CancellationToken` works correctly: cancel mid-render and verify clean shutdown
- [x] 8.6.4 — Stress test: 100 concurrent renders of different documents

### 8.7 Documentation & Release Preparation

- [x] 8.7.1 — Review and finalize README.md with accurate API examples
- [x] 8.7.2 — Ensure all public API members have XML documentation
- [x] 8.7.3 — Create a "Supported Features" matrix in the docs
- [x] 8.7.4 — Create a "Known Limitations" document
- [x] 8.7.5 — Verify NuGet package metadata is correct
- Deferred backlog: [Issue #24](https://github.com/panoramicdata/PanoramicData.Render/issues/24) — tag and publish v1.0.0

## Exit Criteria

- Visual regression tests pass for all corpus documents within defined thresholds
- Performance targets are met (verified by benchmarks in CI)
- Memory targets are met (verified by profiling)
- Error tolerance is verified: no crashes on malformed input
- Thread safety is verified under contention
- API is documented with XML docs and README examples
- NuGet package publishes successfully
- All tests pass; zero warnings

## Known Risks

- Visual regression baselines are tied to a specific Word version. Word updates may shift baselines.
- SVG rasterization for comparison may differ across platforms; pin the rasterization tool and version.
- Performance optimization may conflict with code clarity; document optimizations carefully.
- The "torture test" corpus will reveal features and edge cases not anticipated in Phases 1–7; budget for backtrack fixes.
