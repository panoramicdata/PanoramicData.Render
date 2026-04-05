# Phase 1: Foundation

**Depends on:** Nothing
**Unlocks:** Phase 2 (Text Layout)

## Objective

Establish the project infrastructure, load OpenXML documents into a workable internal representation, implement the full OOXML style cascade, and build the font resolution infrastructure.

## Steps

### 1.1 Project Scaffolding

- [x] 1.1.1 — Solution structure: `PanoramicData.Render.slnx`, main project, test project
- [x] 1.1.2 — CI/CD: GitHub Actions build, test, pack, publish workflow
- [x] 1.1.3 — Central package management (`Directory.Packages.props`)
- [x] 1.1.4 — Nerdbank.GitVersioning, `.editorconfig`, `.codacy.yml`, `SECURITY.md`, `CONTRIBUTING.md`

### 1.2 OpenXML Ingestion

- [x] 1.2.1 — Load a DOCX stream via Open-XML-SDK; extract document body, styles part, theme part, numbering part, settings part
- [x] 1.2.2 — Parse section properties (page size, margins, orientation, header/footer references)
- [x] 1.2.3 — Parse paragraph elements into an internal `DocumentBlock` model (paragraph, table placeholder, section break)
- [x] 1.2.4 — Parse run elements into an internal `TextRun` model (text content, break characters, inline images)
- [x] 1.2.5 — Extract embedded images and media from relationships/parts
- [x] 1.2.6 — Parse header and footer parts (defer layout to Phase 3, but load the content here)
- [x] 1.2.7 — Parse footnote and endnote definitions (defer layout to Phase 3)

### 1.3 Style Resolution

- [x] 1.3.1 — Parse `w:docDefaults` for base paragraph and run properties
- [x] 1.3.2 — Parse theme part: theme fonts (`majorFont`/`minorFont`), theme color scheme
- [ ] 1.3.3 — Resolve theme colors with tint/shade modifiers to concrete RGB values
- [ ] 1.3.4 — Build the paragraph style hierarchy: parse all `w:style` elements, link via `w:basedOn`, resolve inheritance chains
- [ ] 1.3.5 — Build the character style hierarchy (same `basedOn` chaining)
- [ ] 1.3.6 — Implement **toggle property** logic: bold, italic, caps, smallCaps, strike, dstrike, vanish, emboss, imprint, outline, shadow
- [ ] 1.3.7 — Implement numbering style resolution: abstract numbering → numbering instance → level overrides
- [ ] 1.3.8 — Implement table style resolution: table style → conditional formatting bands (first row, last column, banded rows, etc.)
- [ ] 1.3.9 — Compute **effective formatting** for any given paragraph + run: walk the full cascade (doc defaults → theme → numbering → table → paragraph chain → character chain → toggles → direct formatting)
- [ ] 1.3.10 — Unit tests: verify cascade produces correct results for at least 20 carefully constructed test cases covering each cascade level and toggle interactions

### 1.4 Font Infrastructure

- [ ] 1.4.1 — Implement `FontResolver`: scan configured directories for `.ttf`, `.otf`, `.ttc` files; build an index of family name → file path
- [ ] 1.4.2 — Handle TrueType Collections (`.ttc`): enumerate faces within a collection
- [ ] 1.4.3 — Implement font substitution mapping (`RenderOptions.FontSubstitutions`)
- [ ] 1.4.4 — Implement fallback chain: requested → substitution → `FallbackFontFamily` → first available sans-serif
- [ ] 1.4.5 — Create `SKTypeface` instances from resolved font files; cache by family+style for reuse
- [ ] 1.4.6 — Resolve theme fonts: map `majorFont`/`minorFont` to concrete family names per script
- [ ] 1.4.7 — Unit tests: verify font resolution, substitution, fallback, and caching

## Exit Criteria

- A DOCX file can be loaded and its content enumerated as typed internal model objects
- The effective formatting for any run can be computed and verified via unit tests
- Fonts referenced in a test document can be resolved (or fallen back) and loaded as `SKTypeface`
- All tests pass; zero warnings

## Known Risks

- The style cascade is the deepest rabbit hole in this project. Budget extra time here.
- Toggle properties are notoriously under-documented; expect to discover edge cases via real-world documents.
- Font collection (`.ttc`) handling may vary across platforms; test on both Windows and Linux.
