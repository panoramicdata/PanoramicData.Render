# Phase 8: Quality & Performance

**Depends on:** Phase 6 (Output Drivers), Phase 7 (Advanced Features)

## Objective

Build the visual regression test suite, optimize performance and memory usage, harden error tolerance, and prepare for production release.

## Steps

### 8.1 Visual Regression Test Suite

- [x] 8.1.1 — Establish baseline generation workflow: document which Word version produces the reference output (pin to a specific build)
- [ ] 8.1.2 — Create reference PNGs: render reference DOCX files to PDF via the pinned Word version, rasterize PDF to PNG at 150 DPI using a pinned PDF rasterizer
- [x] 8.1.3 — Implement SVG-to-PNG rasterization for test comparison (using SkiaSharp or a headless browser)
- [x] 8.1.4 — Implement perceptual image diff: compare rendered PNG against reference PNG using a perceptual diff algorithm (not raw pixel comparison) to avoid false positives from anti-aliasing
- [x] 8.1.5 — Define per-document thresholds: some documents may tolerate more deviation than others
- [ ] 8.1.6 — Integrate into CI: visual regression tests run on every PR; failed diffs are uploaded as artifacts
- [ ] 8.1.7 — Create a visual diff report: HTML page showing baseline, actual, and diff side-by-side

### 8.2 Test Document Corpus

- [ ] 8.2.1 — Create minimal test documents (one feature per document):
  - Basic text formatting
  - Paragraph alignment and indentation
  - Multi-level lists
  - Simple table
  - Merged cells table
  - Auto-fit table
  - Inline images
  - Floating images with wrapping
  - Headers and footers
  - Multi-section with different page sizes
  - Footnotes
  - Columns
  - Tab stops and leaders
  - Watermark
  - RTL text
- [ ] 8.2.2 — Create integration test documents (multiple features combined):
  - A realistic business letter
  - A multi-page report with tables and charts
  - A contract with complex numbering and headers
  - A document with mixed content (text, tables, images, footnotes)
- [ ] 8.2.3 — Store test documents and reference PNGs in a dedicated `test-assets/` directory in the repo

### 8.3 Performance Optimization

- [ ] 8.3.1 — Profile rendering of a 50-page test document: identify hot paths
- [x] 8.3.2 — Optimize font cache: share `SKTypeface` instances across renders; lazy-load fonts
- [ ] 8.3.3 — Optimize style resolution: cache computed effective styles per paragraph/run style combination
- [x] 8.3.4 — Optimize SVG string building: use `StringBuilder` or `ArrayBufferWriter<char>` instead of string concatenation
- [ ] 8.3.5 — Optimize image handling: avoid unnecessary image decoding/re-encoding; stream images where possible
- [ ] 8.3.6 — Add benchmarks using BenchmarkDotNet: measure throughput for simple, medium, and complex documents
- [x] 8.3.7 — Verify performance targets:
  - 1-page simple document: < 500ms
  - 50-page report: < 10s
  - 500-page document: < 120s

### 8.4 Memory Optimization

- [ ] 8.4.1 — Profile memory usage for a document with many large images
- [ ] 8.4.2 — Implement image streaming: don't hold all images in memory simultaneously during layout
- [ ] 8.4.3 — Implement page disposal: after a page is fully rendered and emitted, release its layout data
- [ ] 8.4.4 — Verify peak memory target: < 3× DOCX file size for text-heavy documents
- [ ] 8.4.5 — Run long-running render loop to verify no memory leaks (render 1000 documents, monitor RSS)

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
- [ ] 8.7.6 — Tag and publish v1.0.0

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
