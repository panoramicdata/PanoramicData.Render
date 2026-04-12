# Supported Features

## Text Formatting

| Feature | Supported | Notes |
|---|---|---|
| Bold / Italic / Underline | ✅ | Full style cascade |
| Strikethrough / Double Strikethrough | ✅ | |
| Superscript / Subscript | ✅ | |
| Font family / size | ✅ | Including theme fonts |
| Font color | ✅ | Including theme colors |
| Highlight color | ✅ | All standard Word colors |
| All Caps / Small Caps | ✅ | |
| Character spacing | ✅ | |
| Hidden text | ✅ | Excluded from output |

## Paragraph Formatting

| Feature | Supported | Notes |
|---|---|---|
| Alignment (left, center, right, justified) | ✅ | Knuth-Plass line breaking |
| Indentation (left, right, hanging, first-line) | ✅ | |
| Spacing (before, after, line spacing) | ✅ | |
| Tab stops with leaders | ✅ | Left, center, right, decimal, bar |
| Paragraph borders | ✅ | All border styles |
| Paragraph shading | ✅ | |
| Keep with next / Keep lines together | ✅ | |
| Widow/orphan control | ✅ | |
| Page/column/section break | ✅ | |

## Lists

| Feature | Supported | Notes |
|---|---|---|
| Bulleted lists | ✅ | |
| Numbered lists (decimal, letter, roman) | ✅ | |
| Multi-level lists | ✅ | Up to 9 levels |
| List number continuation | ✅ | |

## Tables

| Feature | Supported | Notes |
|---|---|---|
| Fixed-width tables | ✅ | |
| Auto-fit tables | ✅ | |
| Cell merging (horizontal) | ✅ | GridSpan |
| Cell merging (vertical) | ✅ | vMerge |
| Cell borders | ✅ | All border styles |
| Cell shading | ✅ | |
| Cell margins | ✅ | |
| Cell vertical alignment | ✅ | Top, center, bottom |
| Cell text direction | ✅ | Vertical text rotation |
| Table header rows | ✅ | Repeat across pages |
| Nested tables | ✅ | |
| BiDi (RTL) tables | ✅ | Column order mirroring |

## Images

| Feature | Supported | Notes |
|---|---|---|
| Inline images | ✅ | |
| Floating images | ✅ | With text wrapping |
| Image wrapping modes | ✅ | Tight, square, through, top-bottom |
| Text wrap distance | ✅ | |
| BehindDocument / InFrontOfDocument | ✅ | Z-order |

## Headers & Footers

| Feature | Supported | Notes |
|---|---|---|
| Default header/footer | ✅ | |
| First page header/footer | ✅ | |
| Odd/even headers/footers | ✅ | |
| Page number fields | ✅ | PAGE, NUMPAGES |
| Date/time fields | ✅ | |

## Sections

| Feature | Supported | Notes |
|---|---|---|
| Page size | ✅ | Any custom size |
| Page margins | ✅ | |
| Page orientation | ✅ | Portrait/landscape |
| Section breaks | ✅ | Next, even, odd, continuous |
| Columns | ✅ | |

## Advanced Features

| Feature | Supported | Notes |
|---|---|---|
| Footnotes | ✅ | |
| Endnotes | ✅ | |
| Hyperlinks | ✅ | |
| Bookmarks | ✅ | Named destinations |
| Watermarks | ✅ | Text watermarks |
| RTL / BiDi text | ✅ | With HarfBuzz shaping |
| Content controls (SDT) | ✅ | Block, inline, table-level |
| Custom XML shapes | ✅ | Custom geometry paths |
| Group shapes | ✅ | |
| Preset shapes | ✅ | Rectangle, ellipse, etc. |

## Style Cascade

| Feature | Supported | Notes |
|---|---|---|
| Document defaults | ✅ | |
| Theme fonts/colors | ✅ | |
| Paragraph styles | ✅ | Full hierarchy chain |
| Character styles | ✅ | Full hierarchy chain |
| Toggle properties | ✅ | bold-on-bold = off |
| Direct formatting | ✅ | |
| Numbering styles | ✅ | |
| Table styles | ✅ | |

## Output Formats

| Format | Supported | Notes |
|---|---|---|
| SVG | ✅ | One SVG per page |
| PDF | ✅ | Multi-page via SkiaSharp |
| SVG font embedding | ✅ | TTF via @font-face |
| SVG image embedding | ✅ | Base64 data URIs |
