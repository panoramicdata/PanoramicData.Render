namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using SkiaSharp;
using System.Globalization;
using System.Text;

/// <summary>
/// Emits rendering commands from paginated layout blocks.
/// </summary>
internal static partial class RenderCommandEmitter
{
	private const float AverageGlyphWidthFactor = 10f;
	private const float DefaultListIndentStepTwips = 360f;
	private const float DefaultListTextGapTwips = 240f;
	private const float DefaultWrapStretchRatio = 0.5f;
	private const float MaxWordLikeJustificationRatio = 2.0f;
	private const float DefaultWrapShrinkRatio = 1f / 3f;
	private const float WordLikeWrapWidthRelaxation = 1.0f;
	private const float BaselineAscentFactor = 0.8f;
	private const float MaxBaselineLineHeightFactor = 0.8f;
	private const float LightBackgroundLuminanceThreshold = 0.6f;
	// Keep wrapping bounded so Knuth-Plass remains responsive on large paragraphs.
	private const int MaxWrappedParagraphTokenCount = 48;
	private static readonly RenderColor DefaultTextColor = new(0, 0, 0);
	private static readonly RenderStroke BarTabStroke = new(DefaultTextColor, 8f);

	/// <summary>
	/// Emits drawing commands for all pages.
	/// </summary>
	/// <param name="pages">The paginated layout pages.</param>
	/// <param name="target">The render target that receives drawing commands.</param>
	/// <param name="options">Optional render options.</param>
	public static void EmitDocument(IReadOnlyList<LayoutPage> pages, IRenderTarget target, RenderOptions? options = null)
	{
		ArgumentNullException.ThrowIfNull(pages);
		ArgumentNullException.ThrowIfNull(target);
		var renderTimestampUtc = DateTime.UtcNow;
		var listState = new ListNumberingState();

		foreach (var page in pages)
		{
			EmitPage(page, target, options, pages.Count, renderTimestampUtc, listState);
		}
	}

	/// <summary>
	/// Emits drawing commands for a single page.
	/// </summary>
	/// <param name="page">The page to emit.</param>
	/// <param name="target">The render target that receives drawing commands.</param>
	/// <param name="options">Optional render options.</param>
	/// <param name="totalPageCount">Optional total page count used by NUMPAGES fields.</param>
	/// <param name="renderTimestampUtc">Optional timestamp used by DATE/TIME fields.</param>
	/// <param name="listState">Optional numbering state for ordered and multi-level list sequences.</param>
	/// <param name="images">Optional pre-loaded image data keyed by relationship ID.</param>
	/// <param name="styles">Optional cloned document styles for table-style resolution.</param>
	public static void EmitPage(LayoutPage page, IRenderTarget target, RenderOptions? options = null, int? totalPageCount = null, DateTime? renderTimestampUtc = null, ListNumberingState? listState = null, IReadOnlyDictionary<string, ImageData>? images = null, Styles? styles = null)
	{
		ArgumentNullException.ThrowIfNull(page);
		ArgumentNullException.ThrowIfNull(target);

		var context = CreatePageContext(page, target, options, totalPageCount, renderTimestampUtc, listState, images, styles);

		if (page.Watermark is { } watermark)
		{
			EmitWatermark(page, watermark, target);
		}

		foreach (var placement in GetBlockPlacements(page))
		{
			EmitLayoutBlock(placement.Block, placement, context);
		}

		EmitHeaderFooterBlocks(page.HeaderBlocks, page.HeaderTopTwips, page, context);
		EmitHeaderFooterBlocks(page.FooterBlocks, page.FooterTopTwips, page, context);
	}

	private static BlockEmitContext CreatePageContext(LayoutPage page, IRenderTarget target, RenderOptions? options, int? totalPageCount, DateTime? renderTimestampUtc, ListNumberingState? listState, IReadOnlyDictionary<string, ImageData>? images, Styles? styles)
	{
		var renderOptions = options ?? new RenderOptions();
		var fontFamily = string.IsNullOrWhiteSpace(renderOptions.FallbackFontFamily)
			? "Times New Roman"
			: renderOptions.FallbackFontFamily;
		return new BlockEmitContext(
			target,
			renderOptions,
			new RenderFont(fontFamily, 12f),
			fontFamily,
			new SolidRenderBrush(DefaultTextColor),
			new RenderStroke(DefaultTextColor, 8f),
			page.PageNumber,
			totalPageCount ?? Math.Max(page.PageNumber, 1),
			renderTimestampUtc ?? DateTime.UtcNow,
			listState ?? new ListNumberingState(),
			page.Section,
			images,
			styles);
	}
	private static IReadOnlyList<LayoutBlockPlacement> GetBlockPlacements(LayoutPage page)
	{
		if (page.BlockPlacements.Count > 0)
		{
			return page.BlockPlacements;
		}

		var contentWidth = page.Section.PageWidth - page.Section.MarginLeft - page.Section.MarginRight;
		var result = new LayoutBlockPlacement[page.Blocks.Count];
		var currentY = page.ContentTopTwips;
		for (var i = 0; i < page.Blocks.Count; i++)
		{
			var block = page.Blocks[i];
			result[i] = new LayoutBlockPlacement(block, page.Section.MarginLeft, currentY, contentWidth, 0);
			currentY += block.HeightTwips;
		}

		return result;
	}

	private static void EmitLayoutBlock(LayoutBlock layoutBlock, LayoutBlockPlacement placement, BlockEmitContext context)
	{
		if (layoutBlock.Block is ParagraphBlock paragraphBlock)
		{
			EmitParagraphBlock(paragraphBlock, layoutBlock, placement, context);
		}
		else if (layoutBlock.Block is TablePlaceholderBlock tableBlock)
		{
			EmitTableBlock(tableBlock, layoutBlock, placement, context);
		}
	}

	private static void EmitParagraphBlock(ParagraphBlock paragraphBlock, LayoutBlock layoutBlock, LayoutBlockPlacement placement, BlockEmitContext context)
	{
		var (target, _, defaultFont, fontFamily, _, _, currentPageNumber, totalPageCount, renderTimestampUtc, _, _, _, _) = context;

		EmitParagraphShadingAndBookmarks(paragraphBlock, layoutBlock, placement, context);

		var logicalSegments = BuildTextSegments(paragraphBlock.SourceElement, defaultFont, fontFamily, currentPageNumber, totalPageCount, renderTimestampUtc);
		if (ShouldEmitWrappedParagraph(paragraphBlock, layoutBlock, logicalSegments, placement.ContentWidthTwips))
		{
			EmitWrappedParagraph(layoutBlock, paragraphBlock, placement, logicalSegments, target);
			EmitParagraphImagesIfAny(paragraphBlock, placement, context);
			return;
		}

		var dominantFontSize = logicalSegments.Count > 0 ? logicalSegments[0].Font.SizePoints : defaultFont.SizePoints;
		var baselineOffset = ComputeBaselineOffsetTwips(dominantFontSize, layoutBlock.HeightTwips);
		var baselineY = placement.YTwips + layoutBlock.SpaceBefore + baselineOffset;
		var segments = BiDiReorderer.Reorder(logicalSegments, static s => s.IsRtl, paragraphBlock.IsBiDi);

		var currentX = placement.XTwips + paragraphBlock.Indentation.GetFirstLineLeftIndent();
		currentX = EmitListLabel(paragraphBlock, placement, context, logicalSegments, baselineY, currentX);
		currentX = ResolveAlignedStartX(paragraphBlock, placement, segments, currentX);
		EmitInlineSegments(segments, paragraphBlock, placement, baselineY, currentX, target);

		EmitBarTabStops(paragraphBlock, placement, placement.YTwips, layoutBlock.HeightTwips, target);

		if (paragraphBlock.Borders.HasAnyVisibleBorder)
		{
			EmitParagraphBorders(paragraphBlock.Borders, placement.XTwips, placement.YTwips, layoutBlock.HeightTwips, placement.ContentWidthTwips, target);
		}

		EmitParagraphImagesIfAny(paragraphBlock, placement, context);
	}
}

