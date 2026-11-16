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

	private static void EmitParagraphShadingAndBookmarks(ParagraphBlock paragraphBlock, LayoutBlock layoutBlock, LayoutBlockPlacement placement, BlockEmitContext context)
	{
		var target = context.Target;
		var shading = ResolveParagraphShading(context.Styles, paragraphBlock.StyleId, paragraphBlock.SourceElement);
		if (shading.HasVisibleShading && TryParseRenderColor(shading.GetEffectiveBackgroundColor(), out var shadingFillColor))
		{
			target.DrawRect(
				new RenderRect(placement.XTwips, placement.YTwips, placement.ContentWidthTwips, layoutBlock.HeightTwips),
				new SolidRenderBrush(shadingFillColor),
				null);
		}

		foreach (var bookmark in paragraphBlock.BookmarkStarts)
		{
			target.SetNamedDestination(bookmark.Name, placement.XTwips, placement.YTwips);
		}
	}

	private static void EmitParagraphImagesIfAny(ParagraphBlock paragraphBlock, LayoutBlockPlacement placement, BlockEmitContext context)
	{
		if (context.Images is { Count: > 0 } images)
		{
			EmitParagraphImages(paragraphBlock, placement, context.Target, context.DefaultFont, context.Section, images);
		}
	}

	private static float EmitListLabel(
		ParagraphBlock paragraphBlock,
		LayoutBlockPlacement placement,
		BlockEmitContext context,
		IReadOnlyList<TextSegment> logicalSegments,
		float baselineY,
		float currentX)
	{
		if (paragraphBlock.NumberingId is not int numberingId || paragraphBlock.NumberingLevel is not int numberingLevel)
		{
			return currentX;
		}

		var listStyle = ResolveListStyle(context.RenderOptions, numberingId, numberingLevel);
		var labelText = AdvanceListLabelText(context, numberingId, listStyle);
		if (string.IsNullOrEmpty(labelText))
		{
			return currentX;
		}

		var (labelFont, labelBrush) = ResolveLabelStyle(logicalSegments, listStyle, context);
		var labelWidth = EstimateTextWidthTwips(labelText, labelFont.SizePoints);
		var (labelX, textStartX) = ResolveListLabelPosition(paragraphBlock, placement, listStyle, numberingLevel, labelWidth);
		context.Target.DrawText(labelText, labelX, baselineY, labelFont, labelBrush);
		return textStartX;
	}

	private static string AdvanceListLabelText(BlockEmitContext context, int numberingId, NumberingLevelStyle listStyle)
	{
		var canonicalNumberingId = context.RenderOptions.NumberingIdNormalization.TryGetValue(numberingId, out var cid) ? cid : numberingId;
		var labelResult = context.ListState.Advance(canonicalNumberingId, listStyle);
		return string.IsNullOrEmpty(labelResult.Label) ? string.Empty : labelResult.Label + " ";
	}

	private static (RenderFont Font, RenderBrush Brush) ResolveLabelStyle(IReadOnlyList<TextSegment> logicalSegments, NumberingLevelStyle listStyle, BlockEmitContext context)
	{
		TextSegment? labelStyleSource = logicalSegments.Any(IsLabelStyleSource)
			? logicalSegments.First(IsLabelStyleSource)
			: null;
		return (
			ResolveLabelFont(labelStyleSource?.Font ?? context.DefaultFont, listStyle),
			labelStyleSource?.Brush ?? context.DefaultBrush);
	}
	private static bool IsLabelStyleSource(TextSegment segment) => !segment.IsTab && !string.IsNullOrEmpty(segment.Text);

	private static RenderFont ResolveLabelFont(RenderFont baseFont, NumberingLevelStyle listStyle)
		=> string.IsNullOrWhiteSpace(listStyle.FontFamily) ? baseFont : baseFont with { Family = listStyle.FontFamily };

	private static (float LabelX, float TextStartX) ResolveListLabelPosition(
		ParagraphBlock paragraphBlock,
		LayoutBlockPlacement placement,
		NumberingLevelStyle listStyle,
		int numberingLevel,
		float labelWidth)
	{
		if (paragraphBlock.IsBiDi)
		{
			var labelX = placement.XTwips + placement.ContentWidthTwips - ((numberingLevel + 1) * DefaultListIndentStepTwips);
			return (labelX, labelX - DefaultListTextGapTwips - labelWidth);
		}

		if (paragraphBlock.Indentation.Hanging > 0f || paragraphBlock.Indentation.Left > 0f)
		{
			var textStartX = placement.XTwips + Math.Max(0f, paragraphBlock.Indentation.Left);
			var labelX = paragraphBlock.Indentation.Hanging > 0f
				? placement.XTwips + Math.Max(0f, paragraphBlock.Indentation.Left - paragraphBlock.Indentation.Hanging)
				: Math.Max(placement.XTwips, textStartX - labelWidth - DefaultListTextGapTwips);
			return (labelX, textStartX);
		}

		if (listStyle.IndentLeftTwips is { } indentLeft)
		{
			var hangingTwips = listStyle.HangingTwips ?? 0f;
			return (placement.XTwips + (indentLeft - hangingTwips), placement.XTwips + indentLeft);
		}

		var defaultTextStartX = placement.XTwips + ((numberingLevel + 1) * DefaultListIndentStepTwips) + DefaultListTextGapTwips;
		return (defaultTextStartX - labelWidth, defaultTextStartX);
	}

	private static float ResolveAlignedStartX(ParagraphBlock paragraphBlock, LayoutBlockPlacement placement, IReadOnlyList<TextSegment> segments, float currentX)
	{
		var effectiveAlignment = paragraphBlock.Alignment
			?? (paragraphBlock.IsBiDi ? ParagraphAlignment.Right : ParagraphAlignment.Left);
		if (paragraphBlock.NumberingId is not null || effectiveAlignment is not (ParagraphAlignment.Right or ParagraphAlignment.Center))
		{
			return currentX;
		}

		var totalWidth = ComputeTotalSegmentWidth(segments);
		return effectiveAlignment is ParagraphAlignment.Right
			? placement.XTwips + placement.ContentWidthTwips - totalWidth
			: placement.XTwips + ((placement.ContentWidthTwips - totalWidth) / 2f);
	}

	private static void EmitInlineSegments(
		IReadOnlyList<TextSegment> segments,
		ParagraphBlock paragraphBlock,
		LayoutBlockPlacement placement,
		float baselineY,
		float currentX,
		IRenderTarget target,
		bool includeHyperlinks = true)
	{
		var tabProfile = TabStopParser.ParseTabStops(paragraphBlock.SourceElement.ParagraphProperties);
		for (var segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
		{
			var segment = segments[segmentIndex];
			if (segment.IsTab)
			{
				currentX = ResolveTabAdvance(segments, segmentIndex, tabProfile, placement, currentX, baselineY, target);
				continue;
			}

			var segmentWidth = EstimateTextWidthTwips(segment.Text, segment.Font.SizePoints);
			DrawTextRun(target, new TextDrawing(segment.Text, segment.Font, segment.Brush, segment.HighlightFillColor, includeHyperlinks ? segment.HyperlinkUri : null), currentX, baselineY, segmentWidth);
			currentX += segmentWidth;
		}
	}

	private static float ResolveTabAdvance(
		IReadOnlyList<TextSegment> segments,
		int segmentIndex,
		TabStopProfile tabProfile,
		LayoutBlockPlacement placement,
		float currentX,
		float baselineY,
		IRenderTarget target)
	{
		var segment = segments[segmentIndex];
		var leaderStartX = currentX;
		if (segment.IsPtabRightMargin)
		{
			var contentAfterTab = GetTextAfterTab(segments, segmentIndex);
			var contentWidthAfterTab = EstimateTextWidthTwips(contentAfterTab, segment.Font.SizePoints);
			return placement.XTwips + placement.ContentWidthTwips - contentWidthAfterTab;
		}

		var tabStop = tabProfile.ResolveNextTabStop(currentX - placement.XTwips);
		var newX = placement.XTwips + ResolveTabStopOffset(tabStop, GetTextAfterTab(segments, segmentIndex), segment.Font.SizePoints);
		EmitLeaderCharacters(tabStop.Leader, leaderStartX, newX, baselineY, segment.Font, segment.Brush, target);
		return newX;
	}

	private static float ResolveTabStopOffset(TabStop tabStop, string contentAfterTab, float fontSizePoints)
	{
		if (tabStop.Type == TabStopType.Decimal)
		{
			var decimalIndex = contentAfterTab.IndexOf(TabStopResolver.DecimalSeparator);
			var widthBeforeDecimal = decimalIndex >= 0
				? EstimateTextWidthTwips(contentAfterTab[..decimalIndex], fontSizePoints)
				: EstimateTextWidthTwips(contentAfterTab, fontSizePoints);
			return TabStopResolver.ComputeContentStart(tabStop, 0f, widthBeforeDecimal);
		}

		if (tabStop.Type is TabStopType.Right or TabStopType.Center)
		{
			return TabStopResolver.ComputeContentStart(tabStop, EstimateTextWidthTwips(contentAfterTab, fontSizePoints));
		}

		return tabStop.PositionTwips;
	}

	private static void DrawTextRun(IRenderTarget target, TextDrawing text, float x, float baselineY, float widthTwips)
	{
		if (text.HighlightFillColor is { } highlightFillColor)
		{
			var highlightTopOffset = EstimateHighlightTopOffsetTwips(text.Font.SizePoints);
			var highlightHeight = EstimateHighlightHeightTwips(text.Font.SizePoints);
			target.DrawRect(new RenderRect(x, baselineY - highlightTopOffset, widthTwips, highlightHeight), new SolidRenderBrush(highlightFillColor), null);
		}

		target.DrawText(text.Text, x, baselineY, text.Font, text.Brush);
		if (!string.IsNullOrWhiteSpace(text.HyperlinkUri))
		{
			var textHeight = EstimateTextHeightTwips(text.Font.SizePoints);
			target.SetHyperlink(new RenderRect(x, baselineY - textHeight, widthTwips, textHeight), text.HyperlinkUri);
		}
	}
}

