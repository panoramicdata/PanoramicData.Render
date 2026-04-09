# PanoramicData.Render — Implementation Plan

## Numbering Convention

Phases use hierarchical numbering (e.g., `1.2.3`) so that steps can be inserted without renumbering. If Phase 1 needs additional steps discovered during implementation, they are added as `1.1.5`, `1.1.6`, etc.

## Phase Summary

| Phase | Title | Depends On | Focus |
|---|---|---|---|
| [1](docs/plans/phase-1-foundation.md) | Foundation | — | Project scaffold, OpenXML ingestion, style resolution, font infrastructure |
| [2](docs/plans/phase-2-text-layout.md) | Text Layout | 1 | Measurement engine, Knuth-Plass line breaking, paragraph & character formatting |
| [3](docs/plans/phase-3-page-layout.md) | Page Layout | 2 | Pagination, sections, headers/footers, footnotes/endnotes |
| [4](docs/plans/phase-4-tables.md) | Tables | 3 | Fixed & auto-fit layout, cell merging, borders, nested tables |
| [5](docs/plans/phase-5-graphics.md) | Graphics & Objects | 3 | Inline/floating images, wrapping, DrawingML shapes, charts |
| [6](docs/plans/phase-6-output-drivers.md) | Output Drivers | 2 | SVG renderer, PDF renderer, font embedding |
| [7](docs/plans/phase-7-advanced-features.md) | Advanced Features | 4, 5 | Fields, lists, text boxes, columns, bookmarks, watermarks, tab stops, RTL/BiDi |
| [8](docs/plans/phase-8-quality-performance.md) | Quality & Performance | 6, 7 | Visual regression suite, performance/memory optimization, error tolerance |
| [9](docs/plans/phase-9-future-enhancements.md) | Future Enhancements | 8 | Native chart rendering, native SmartArt layout |

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
       ▼
Phase 9: Future Enhancements
```

**Note:** Phase 6 (Output Drivers) can begin as soon as Phase 2 is complete, in parallel with Phases 3–5. A basic SVG driver is valuable early for visual debugging during layout development.

## Guiding Principles

1. **Each step produces testable output.** No step is "done" until it has tests proving it works.
2. **Vertical slices over horizontal layers.** Prefer a thin end-to-end path (simple text → SVG) over completing all of one layer before starting the next.
3. **Known deviations are documented.** If a step is implemented with known limitations, they are logged in the phase document and tracked as issues.
4. **The plan will change.** Steps can be inserted, reordered, or subdivided. The hierarchical numbering supports this.

## Current Status

All phases are **not started**. This document will be updated as work progresses.
