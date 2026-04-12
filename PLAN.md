# PanoramicData.Render — Implementation Plan

## Numbering Convention

Phases use hierarchical numbering (e.g., `1.2.3`) so that steps can be inserted without renumbering. If Phase 1 needs additional steps discovered during implementation, they are added as `1.1.5`, `1.1.6`, etc.

## Phase Summary

| Phase | Title | Status | Depends On | Focus |
|---|---|---|---|---|
| [1](docs/plans/phase-1-foundation.md) | Foundation | Complete | — | Project scaffold, OpenXML ingestion, style resolution, font infrastructure |
| [2](docs/plans/phase-2-text-layout.md) | Text Layout | Complete | 1 | Measurement engine, Knuth-Plass line breaking, paragraph & character formatting |
| [3](docs/plans/phase-3-page-layout.md) | Page Layout | Complete | 2 | Pagination, sections, headers/footers, footnotes/endnotes |
| [4](docs/plans/phase-4-tables.md) | Tables | Complete | 3 | Fixed & auto-fit layout, cell merging, borders, nested tables |
| [5](docs/plans/phase-5-graphics.md) | Graphics & Objects | Complete | 3 | Inline/floating images, wrapping, DrawingML shapes, charts |
| [6](docs/plans/phase-6-output-drivers.md) | Output Drivers | Complete | 2 | SVG renderer, PDF renderer, font embedding |
| [7](docs/plans/phase-7-advanced-features.md) | Advanced Features | Complete | 4, 5 | Fields, lists, text boxes, columns, bookmarks, watermarks, tab stops, RTL/BiDi |
| [8](docs/plans/phase-8-quality-performance.md) | Quality & Performance | Closed for roadmap; backlog in [#18](https://github.com/panoramicdata/PanoramicData.Render/issues/18)-[#24](https://github.com/panoramicdata/PanoramicData.Render/issues/24) | 6, 7 | Visual regression suite, performance/memory hardening, release backlog |
| [9](docs/plans/phase-9-future-enhancements.md) | Future Enhancements | Deferred to [#25](https://github.com/panoramicdata/PanoramicData.Render/issues/25) and [#26](https://github.com/panoramicdata/PanoramicData.Render/issues/26) | 8 | Native chart rendering and native SmartArt layout |
| [10](docs/plans/phase-10-field-update.md) | Field Update Engine | Planned | 7, 8 | Multi-pass field recalculation for page numbers, TOC, TOF, cross-references, and document properties |
| [11](docs/plans/phase-11-webassembly-demo.md) | WebAssembly Demo | Planned | 10 | Static GitHub Pages SPA, drag/drop DOCX rendering, SVG page strip, and PDF download |

## Dependency Graph

```
Phase 1: Foundation
  │
  ▼
Phase 2: Text Layout
  │
  ├──────────────────┐
  ▼                  ▼
Phase 3: Page Layout   Phase 6: Output Drivers
  │
  ├──────────┐
  ▼          ▼
Phase 4    Phase 5
Tables     Graphics
  │          │
  └────┬─────┘
       ▼
Phase 7: Advanced Features
       │
       ▼
Phase 8: Quality & Performance
       │
       ├── Deferred backlog issues #18-24
       ▼
Phase 10: Field Update Engine
       │
       ▼
Phase 11: WebAssembly Demo

Deferred future work:
Phase 9 -> backlog issues #25-26
```

**Note:** Phase 6 (Output Drivers) can begin as soon as Phase 2 is complete, in parallel with Phases 3–5. A basic SVG driver is valuable early for visual debugging during layout development.

**Roadmap note:** The only remaining active phases are [Phase 10](docs/plans/phase-10-field-update.md) and [Phase 11](docs/plans/phase-11-webassembly-demo.md). Remaining unfinished non-SPA work from Phases 8 and 9 is tracked in GitHub issues instead of open phase checklists.

## Guiding Principles

1. **Each step produces testable output.** No step is "done" until it has tests proving it works.
2. **Vertical slices over horizontal layers.** Prefer a thin end-to-end path (simple text → SVG) over completing all of one layer before starting the next.
3. **Known deviations are documented.** If a step is implemented with known limitations, they are logged in the phase document and tracked as issues.
4. **The plan will change.** Steps can be inserted, reordered, or subdivided. The hierarchical numbering supports this.

## Current Status

Phases 1 through 8 define the current renderer baseline. Remaining non-SPA backlog from [Phase 8](docs/plans/phase-8-quality-performance.md) is tracked in [#18](https://github.com/panoramicdata/PanoramicData.Render/issues/18) through [#24](https://github.com/panoramicdata/PanoramicData.Render/issues/24), and [Phase 9](docs/plans/phase-9-future-enhancements.md) is deferred to [#25](https://github.com/panoramicdata/PanoramicData.Render/issues/25) and [#26](https://github.com/panoramicdata/PanoramicData.Render/issues/26). The remaining active roadmap is the SPA delivery track: [Phase 10](docs/plans/phase-10-field-update.md) followed by [Phase 11](docs/plans/phase-11-webassembly-demo.md).
