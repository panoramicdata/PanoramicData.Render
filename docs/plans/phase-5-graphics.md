# Phase 5: Graphics & Objects

**Depends on:** Phase 3 (Page Layout)
**Unlocks:** Phase 7 (Advanced Features)

## Objective

Implement rendering of images (inline and floating), text wrapping around floating objects, simple geometric shapes from DrawingML, and best-effort rendering of charts and SmartArt.

## Steps

### 5.1 Inline Images

- [x] 5.1.1 — Parse inline drawing elements (`w:drawing` → `wp:inline` → `a:graphic`)
- [x] 5.1.2 — Extract image data from the package (JPEG, PNG, GIF, BMP, TIFF, WMF, EMF)
- [x] 5.1.3 — Handle image sizing: extent specified in EMUs (English Metric Units, 1/914400 inch); convert to twips
- [x] 5.1.4 — Position inline images within the text flow: baseline-aligned, treated as a large glyph for line breaking
- [x] 5.1.5 — Handle image cropping (`a:srcRect`)
- [x] 5.1.6 — Handle WMF/EMF vector formats: rasterize via SkiaSharp or render as SVG paths where possible
- [x] 5.1.7 — Unit tests: verify inline image positioning and sizing

### 5.2 Floating Images

- [x] 5.2.1 — Parse anchor drawing elements (`w:drawing` → `wp:anchor`)
- [x] 5.2.2 — Resolve anchor positioning: relative to page, column, paragraph, character, margin
- [x] 5.2.3 — Handle horizontal/vertical alignment (left, center, right, inside, outside)
- [ ] 5.2.4 — Handle offset positioning (absolute distance from anchor reference)
- [ ] 5.2.5 — Compute the floating image's absolute $(x, y)$ position on the page
- [ ] 5.2.6 — Handle z-order: `behindDoc` (behind text) vs. default (in front of text)
- [ ] 5.2.7 — Unit tests: verify floating image positions relative to different anchor types

### 5.3 Text Wrapping

- [ ] 5.3.1 — **Square wrapping:** Text flows around the image's bounding rectangle with configurable distance (top, bottom, left, right)
- [ ] 5.3.2 — **Tight wrapping:** Text flows around the image's wrap polygon (`wp:wrapTight` → `wp:wrapPolygon`)
- [ ] 5.3.3 — **Top and bottom:** Text stops above and resumes below the image; no text beside it
- [ ] 5.3.4 — **Behind text / In front of text:** No text displacement; image is layered
- [ ] 5.3.5 — Integrate wrapping with the line-breaking engine: reduce available line width in regions occupied by floating objects
- [ ] 5.3.6 — Handle multiple floating objects on the same page with overlapping wrap regions
- [ ] 5.3.7 — Unit tests: verify text wrapping produces correct line widths and positions

### 5.4 DrawingML Shapes

- [ ] 5.4.1 — Parse preset geometries (`a:prstGeom`): rectangles, rounded rectangles, ellipses, arrows, callouts, etc.
- [ ] 5.4.2 — Parse custom geometries (`a:custGeom`): moveTo, lineTo, cubicBezierTo, arcTo, close
- [ ] 5.4.3 — Apply shape fills: solid, gradient (linear/radial), pattern, picture fill
- [ ] 5.4.4 — Apply shape outlines: width, color, dash style, join style
- [ ] 5.4.5 — Handle shape text frames: text content inside shapes, with internal margins and auto-fit
- [ ] 5.4.6 — Handle shape rotation and flipping
- [ ] 5.4.7 — Handle grouped shapes (`wpg:wgp`): recursive group with relative transforms
- [ ] 5.4.8 — Unit tests: verify shape rendering for at least the 10 most common preset geometries

### 5.5 Charts (Best-Effort)

- [ ] 5.5.1 — Detect chart elements (`c:chartSpace` in chart parts)
- [ ] 5.5.2 — If a fallback image is embedded in the chart part, render the fallback image
- [ ] 5.5.3 — If no fallback image: render a placeholder rectangle with "Chart" label
- [ ] 5.5.4 — Future: implement native chart rendering (bar, line, pie) — tracked as a separate feature request

### 5.6 SmartArt (Best-Effort)

- [ ] 5.6.1 — Detect SmartArt elements
- [ ] 5.6.2 — If a DrawingML fallback is present in the package, render the fallback shapes
- [ ] 5.6.3 — If no fallback: render a placeholder rectangle with "SmartArt" label
- [ ] 5.6.4 — Future: implement native SmartArt layout — tracked as a separate feature request

### 5.7 OLE Objects (Best-Effort)

- [ ] 5.7.1 — Detect OLE embedded objects
- [ ] 5.7.2 — If a preview image (EMF/WMF) is available, render the preview
- [ ] 5.7.3 — If no preview: render a placeholder rectangle

## Exit Criteria

- Inline images render at the correct size and position within text flow
- Floating images render at the correct absolute position with appropriate wrapping
- Text wrapping around floating objects produces correct line widths
- Common DrawingML shapes render with correct geometry, fill, and outline
- Charts and SmartArt degrade gracefully (fallback image or placeholder)
- All tests pass; zero warnings

## Known Risks

- EMU-to-twip conversion precision: EMUs are finer-grained than twips. Rounding must be consistent.
- WMF/EMF rendering is platform-dependent; SkiaSharp's support varies. May need a dedicated WMF/EMF parser.
- Tight wrapping polygons can be concave and complex; line-width reduction must handle arbitrary polygon intersection with horizontal lines.
- The number of preset geometries in DrawingML is large (~200). Prioritize the most common ones; log warnings for unimplemented geometries.
