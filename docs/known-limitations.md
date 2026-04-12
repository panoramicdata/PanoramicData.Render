# Known Limitations

## PDF Output

- **No tagged PDF:** The PDF output is not tagged/accessible (no PDF/UA support).
- **No font subsetting:** Embedded fonts are not subsetted, resulting in larger PDF files.
- **No PDF/A compliance:** Output does not conform to PDF/A archival standards.
- **No digital signatures:** PDF digital signing is not supported.

## Document Format

- **DOCX only:** Binary `.doc` format (OLE2) is not supported and never will be.
- **No macro support:** VBA macros are ignored.
- **No embedded OLE objects:** Embedded Excel/PowerPoint objects are not rendered.
- **No SmartArt:** SmartArt diagrams are not rendered (rendered as a static image if a fallback image part exists).
- **No chart rendering:** Chart objects are not rendered (rendered as a static image if a fallback image part exists).
- **No math equations (OMML):** Office Math Markup Language equations are not rendered.

## Text Layout

- **Complex script limitations:** While HarfBuzz handles shaping, some complex script features (e.g., contextual alternates requiring OpenType feature flags) may not be fully rendered.
- **Hyphenation:** TeX-style hyphenation is available but may not match Word's built-in hyphenation exactly.
- **Kerning:** Basic kerning via font metrics is supported, but optical kerning adjustments may differ from Word.

## Images

- **WMF/EMF limitations:** Windows Metafile formats may not render with full fidelity on non-Windows platforms.
- **SVG images:** SVG images embedded in DOCX are supported but complex SVG features may not render.

## Tables

- **Table autofit:** Auto-fit algorithm approximates Word's behavior but may differ for edge cases with mixed percentage/fixed widths.
- **Conditional formatting:** Table style conditional formatting (first row, last column, etc.) is partially supported.

## Performance

- **Memory for large images:** Documents with many large images may consume significant memory during rendering.
- **Complex documents:** Documents with hundreds of pages and complex layouts (deeply nested tables, many floating images) may take longer than simple text-heavy documents.

## Platform

- **Font availability:** Rendering fidelity depends on having the same fonts available as the document's author used. Font substitution is available but may change text metrics.
- **SkiaSharp native dependencies:** Requires SkiaSharp native binaries for the target platform (Windows, Linux, macOS).

## Output Fidelity

- **Not pixel-perfect:** The goal is high fidelity, not pixel-perfect reproduction of Word's rendering. Minor differences in text positioning, line breaking, and spacing are expected.
- **Font hinting differences:** Sub-pixel rendering and font hinting may differ from Word's rendering engine.
