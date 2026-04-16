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

## Rendering Issue Investigation Protocol

When investigating a visual rendering issue (wrong position, wrong color, wrong size, missing feature, etc.), **always follow this protocol before writing any code**:

1. **Assume the answer is in the DOCX.** Every visual property in a DOCX is controlled by XML — spacing, fonts, colors, table styles, numbering, field codes, run properties, paragraph properties, tab stops, etc. The data to produce the correct output is always there, either as direct formatting, inherited from a style, inherited via the style chain (docDefaults → theme → numbered → table → paragraph style → character style → direct), or as a currently-ignored attribute.

2. **Trace the full cascade.** Before concluding something is a bug, inspect the raw XML of the DOCX part that contains the element in question (the paragraph, run, table cell, header/footer part, etc.). Use `DocumentFormat.OpenXml.Packaging.WordprocessingDocument` to examine the raw XML, or unzip the DOCX and read the XML directly. Look for:
   - Direct run/paragraph properties (`<w:rPr>`, `<w:pPr>`)
   - Paragraph style and character style references (`<w:pStyle>`, `<w:rStyle>`)
   - Numbering definitions (`<w:numPr>`, `numbering.xml`)
   - Table styles and cell properties (`<w:tblStyle>`, `<w:trPr>`, `<w:tcPr>`)
   - Document defaults (`<w:docDefaults>` in `styles.xml`)
   - Theme data (`theme/theme1.xml`)
   - Header/footer parts (`word/header*.xml`, `word/footer*.xml`)
   - Field codes (`<w:fldChar>`, `<w:instrText>`)

3. **Ask the user for help when needed.** If you cannot determine the correct value from the DOCX XML alone:
   - Ask for a screenshot of the area in Word that exhibits the expected behaviour.
   - Ask the user to open the DOCX in Word and describe what they see in a particular style panel, format dialog, or ruler.
   - Ask the user to provide an XML snippet from a specific path if you cannot unzip/read it yourself.

4. **Never guess at hard-coded values.** If you can't determine a value from the DOCX data and cascade, ask before implementing.

5. **Write a failing test first.** Every fix must be accompanied by a test that verifies the correct value is produced from the DOCX. The test should read the specific property from the rendered output and assert the expected value.

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

### Test Policy

**NEVER skip, suppress, or work around failing tests.** This is non-negotiable:

- Tests that fail are exposing real bugs. Silencing them delays discovery and accumulates technical debt.
- If a test cannot be fixed immediately, create a GitHub Issue to track it with:
  - Clear description of root cause
  - Reproduction steps
  - Expected vs actual behavior
- Remove the test from "known gaps" or skip lists only when the issue is FIXED, not when we decide to ignore it
- Every failing test must have a corresponding GitHub Issue, with:
  - `status/known-bug` label if not yet worked on
  - Link to the GitHub Issue in code comments if temporarily skipped
  - Removal of skip/suppress code immediately when fixed

This applies to all test categories: unit tests, integration tests, visual regression tests, performance tests.

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
