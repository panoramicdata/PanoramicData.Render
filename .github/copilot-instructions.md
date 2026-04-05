# Copilot Instructions for PanoramicData.Render

## Project Overview

PanoramicData.Render is a high-fidelity rendering engine that converts OpenXML (DOCX) documents to paginated SVG and PDF output. It acts as a virtual layout engine — calculating exact glyph positions, line breaks, and object anchors — rather than a semantic HTML converter.

**Primary use case:** DOCX → SVG conversion for web-based document viewing.

## Architecture

The library follows a **Measure-then-Paint** pipeline:

1. **DOM Ingestion** — Open-XML-SDK loads DOCX parts
2. **Style Resolution** — Full OOXML cascade (doc defaults → theme → numbering → table → paragraph/character style chains → toggle properties → direct formatting)
3. **Layout Engine** — SkiaSharp + HarfBuzz for font metrics; Knuth-Plass line breaking; pagination
4. **Render Abstraction** — `IRenderTarget` interface for drawing commands
5. **Output Drivers** — `SvgRenderTarget` and `PdfRenderTarget`

## Key Technical Decisions

- **Internal unit:** Twips (1/1440 inch) — matches Word's internal precision
- **Line breaking:** Knuth-Plass algorithm (paragraph-optimal, not greedy)
- **Font measurement:** SkiaSharp with HarfBuzz (`SKShaper`) for complex script shaping
- **PDF backend:** SkiaSharp `SKDocument.CreatePdf()` (known limitations: no tagged PDF, no font subsetting, no PDF/A)
- **SVG fonts:** Optional WOFF2 embedding via `@font-face`
- **Style cascade:** Full OOXML cascade including toggle property semantics (bold on bold = bold OFF)

## Coding Standards

This project follows the PanoramicData NugetManagement standards:

- **Target:** .NET 10.0
- **Nullable:** Enabled, treat warnings as errors
- **Namespaces:** File-scoped (`file namespace Foo;`) — enforced as ERROR
- **Indentation:** Tabs, size 4
- **Naming:**
  - Interfaces: `I` prefix (e.g., `IRenderTarget`)
  - Private fields: `_camelCase`
  - Everything else: `PascalCase`
- **Accessibility modifiers:** Required on all non-interface members (ERROR)
- **XML documentation:** Required on all public members
- **JSON:** Use `System.Text.Json`, never `Newtonsoft.Json`
- **Logging:** `Microsoft.Extensions.Logging.Abstractions` — no `Console.Write` or `Trace`
- **Testing:** xUnit v3 + AwesomeAssertions
- **Package management:** Central (`Directory.Packages.props`) — never inline version numbers in .csproj

## Project Structure

```
PanoramicData.Render/                 # Main library (NuGet package)
PanoramicData.Render.Test/            # Test project (xUnit v3)
docs/plans/                           # Phased implementation plans
```

## What NOT to Do

- Do not use greedy line breaking. The project uses Knuth-Plass.
- Do not use HTML as an intermediate format. Output is SVG and PDF only.
- Do not add `.doc` (binary Word format) support. DOCX only.
- Do not add Newtonsoft.Json.
- Do not suppress warnings without justification.
- Do not use string concatenation for SVG building — use `StringBuilder` or XML APIs.
- Do not hold all images in memory simultaneously — stream where possible.
- Do not skip the style cascade. Every text run's formatting must be resolved through the full cascade.

## Non-Functional Requirements

- **Performance:** < 500ms for 1 page, < 10s for 50 pages, < 120s for 500 pages
- **Memory:** Peak < 3× DOCX file size for text-heavy documents
- **Thread safety:** `DocxRenderer` must be safe for concurrent use
- **Error tolerance:** Malformed input → best-effort rendering + warnings, not exceptions
- **Cancellation:** `CancellationToken` throughout the pipeline

## Development Methodology

This project uses **Spec-Driven Development** combined with **Test-Driven Development (TDD)**:

1. **Spec first:** Every feature is defined in `DESIGN.md` and the phase plan docs before implementation begins
2. **Tests first:** Write failing tests that verify the spec, then implement to make them pass
3. **Full coverage:** 100% code coverage is required at every commit — enforced in CI
4. **All tests pass:** No commit may break existing tests

### Commit Rules

Every commit must:
- Pass all unit tests
- Maintain 100% code coverage (line coverage via coverlet)
- Build with zero warnings
- Have the phase plan docs updated to reflect progress (checkboxes ticked)

### Spec Changes

The spec (`DESIGN.md`, phase docs) can change over time, but only deliberately:
- Propose the change with rationale
- Get agreement before implementing
- Update the spec docs first, then update implementation and tests

## Session Continuation Workflow

When a new session starts and the user says **"continue"**, follow this workflow:

1. **Read `PLAN.md`** and the phase plan docs (`docs/plans/phase-*.md`) to determine the current phase and step
2. **Check `docs/plans/current-status.md`** for the last recorded working state, including any in-progress implementation notes
3. **Check GitHub Issues** for any externally-reported requests or bugs
4. **Record intent:** Update `docs/plans/current-status.md` with what you're about to work on
5. **Review code state:** If the last session was interrupted mid-implementation, check for partially-written code, failing tests, or uncommitted changes
6. **Work on the next step** using TDD:
   - Write/update tests for the step
   - Implement to pass the tests
   - Verify 100% coverage
   - Update the phase doc checkbox
7. **Update `docs/plans/current-status.md`** with progress after each step
8. **Commit** with a descriptive message referencing the step number (e.g., "Implement step 1.2.3: Parse section properties")

### Status Tracking

`docs/plans/current-status.md` is the single source of truth for session continuity. It records:
- Current phase and step being worked on
- Implementation notes and decisions made
- Any blockers or questions
- What was last committed

This file is updated frequently during work and committed with each change.

## Key Documents

- `DESIGN.md` — Full architecture and technical design
- `PLAN.md` — Phased implementation roadmap
- `docs/plans/phase-*.md` — Detailed per-phase deliverables with hierarchical step numbering
- `docs/plans/current-status.md` — Current working state for session continuity

## Dependencies

| Package | Purpose |
|---|---|
| `DocumentFormat.OpenXml` | OOXML parsing |
| `SkiaSharp` | Font metrics, image processing, PDF backend |
| `SkiaSharp.HarfBuzz` | Complex script shaping |
| `Microsoft.Extensions.Logging.Abstractions` | Structured logging |

All dependencies are MIT-licensed.
