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
internal static class RenderCommandEmitter
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

		var renderOptions = options ?? new RenderOptions();
		var fontFamily = string.IsNullOrWhiteSpace(renderOptions.FallbackFontFamily)
			? "Times New Roman"
			: renderOptions.FallbackFontFamily;
		var defaultFont = new RenderFont(fontFamily, 12f);
		var defaultBrush = new SolidRenderBrush(DefaultTextColor);
		var defaultStroke = new RenderStroke(DefaultTextColor, 8f);
		var effectiveTotalPageCount = totalPageCount ?? Math.Max(page.PageNumber, 1);
		var effectiveTimestampUtc = renderTimestampUtc ?? DateTime.UtcNow;
		var effectiveListState = listState ?? new ListNumberingState();
		var context = new BlockEmitContext(target, renderOptions, defaultFont, fontFamily, defaultBrush, defaultStroke, page.PageNumber, effectiveTotalPageCount, effectiveTimestampUtc, effectiveListState, page.Section, images, styles);

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

		var renderOptions = context.RenderOptions;
		var canonicalNumberingId = renderOptions.NumberingIdNormalization.TryGetValue(numberingId, out var cid) ? cid : numberingId;
		var listStyle = ResolveListStyle(renderOptions, numberingId, numberingLevel);
		var labelResult = context.ListState.Advance(canonicalNumberingId, listStyle);
		var labelText = string.IsNullOrEmpty(labelResult.Label) ? string.Empty : labelResult.Label + " ";
		if (string.IsNullOrEmpty(labelText))
		{
			return currentX;
		}

		var hasLabelStyleSource = logicalSegments.Any(segment => !segment.IsTab && !string.IsNullOrEmpty(segment.Text));
		var labelStyleSource = hasLabelStyleSource
			? logicalSegments.First(segment => !segment.IsTab && !string.IsNullOrEmpty(segment.Text))
			: default;
		var labelFont = hasLabelStyleSource ? labelStyleSource.Font : context.DefaultFont;
		if (!string.IsNullOrWhiteSpace(listStyle.FontFamily))
		{
			labelFont = labelFont with { Family = listStyle.FontFamily };
		}

		var labelBrush = hasLabelStyleSource ? labelStyleSource.Brush : context.DefaultBrush;
		var labelWidth = EstimateTextWidthTwips(labelText, labelFont.SizePoints);
		var (labelX, textStartX) = ResolveListLabelPosition(paragraphBlock, placement, listStyle, numberingLevel, labelWidth);
		context.Target.DrawText(labelText, labelX, baselineY, labelFont, labelBrush);
		return textStartX;
	}

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

	internal static int EstimateWrappedLineCount(ParagraphBlock paragraphBlock, float availableWidthTwips, string defaultFontFamily = "Times New Roman")
	{
		ArgumentNullException.ThrowIfNull(paragraphBlock);

		if (availableWidthTwips <= 0f)
		{
			return 1;
		}

		var defaultFont = new RenderFont(defaultFontFamily, 12f);
		var segments = BuildTextSegments(paragraphBlock.SourceElement, defaultFont, defaultFontFamily, 1, 1, DateTime.UnixEpoch);
		if (!CanWrapParagraph(paragraphBlock, segments, availableWidthTwips))
		{
			return 1;
		}

		var effectiveAlignment = paragraphBlock.Alignment
			?? (paragraphBlock.IsBiDi ? ParagraphAlignment.Right : ParagraphAlignment.Left);

		return CountWrappedLines(segments, availableWidthTwips, effectiveAlignment);
	}

	private static bool ShouldEmitWrappedParagraph(ParagraphBlock paragraphBlock, LayoutBlock layoutBlock, IReadOnlyList<TextSegment> segments, float availableWidthTwips)
	{
		if (layoutBlock.LineStartIndex > 0)
		{
			return CanWrapParagraph(paragraphBlock, segments, availableWidthTwips);
		}

		return layoutBlock.LineHeights is { Count: > 1 }
			&& CanWrapParagraph(paragraphBlock, segments, availableWidthTwips);
	}

	private static bool CanWrapParagraph(ParagraphBlock paragraphBlock, IReadOnlyList<TextSegment> segments, float availableWidthTwips)
	{
		if (availableWidthTwips <= 0f || paragraphBlock.IsBiDi || paragraphBlock.NumberingId is not null)
		{
			return false;
		}

		for (var i = 0; i < segments.Count; i++)
		{
			if (segments[i].IsTab || segments[i].IsRtl)
			{
				return false;
			}
		}

		return CountApproximateWrapTokens(segments) <= MaxWrappedParagraphTokenCount;
	}

	private static void EmitWrappedParagraph(
		LayoutBlock layoutBlock,
		ParagraphBlock paragraphBlock,
		LayoutBlockPlacement placement,
		IReadOnlyList<TextSegment> logicalSegments,
		IRenderTarget target)
	{
		var effectiveAlignment = paragraphBlock.Alignment ?? ParagraphAlignment.Left;
		var wrappedLines = BuildWrappedLines(logicalSegments, placement.ContentWidthTwips, effectiveAlignment, paragraphBlock.Indentation);
		if (wrappedLines.Count == 0)
		{
			return;
		}

		var lineHeights = layoutBlock.LineHeights ?? [layoutBlock.HeightTwips];
		var lineStartIndex = layoutBlock.LineStartIndex;
		if (lineStartIndex >= wrappedLines.Count)
		{
			return;
		}

		var lineCount = Math.Min(lineHeights.Count, wrappedLines.Count - lineStartIndex);
		var currentY = placement.YTwips + layoutBlock.SpaceBefore;
		for (var lineIndex = 0; lineIndex < lineCount; lineIndex++)
		{
			var lineHeight = lineHeights[lineIndex];
			var lineSegments = wrappedLines[lineStartIndex + lineIndex].Segments;
			var lineFontSize = lineSegments.Count > 0 ? lineSegments[0].Font.SizePoints : 12f;
			var baselineY = currentY + ComputeBaselineOffsetTwips(lineFontSize, lineHeight);
			foreach (var segment in lineSegments)
			{
				DrawTextRun(target, new TextDrawing(segment.Text, segment.Font, segment.Brush, segment.HighlightFillColor, segment.HyperlinkUri), placement.XTwips + segment.XOffset, baselineY, segment.WidthTwips);

			}

			currentY += lineHeight;
		}
	}

	private static IReadOnlyList<WrappedLine> BuildWrappedLines(
		IReadOnlyList<TextSegment> segments,
		float lineWidthTwips,
		ParagraphAlignment alignment,
		ParagraphIndentation indentation = default)
	{
		if (segments.Count == 0 || lineWidthTwips <= 0f)
		{
			return [];
		}

		var tokens = TokenizeWrappedSegments(segments);
		if (tokens.Count == 0)
		{
			return [];
		}

		var (items, itemTokenIndexes) = BuildWrapItems(tokens);
		var plan = new WrapPlan(tokens, items, itemTokenIndexes);

		var wrapWidthTwips = lineWidthTwips * WordLikeWrapWidthRelaxation;
		var lineBreaks = KnuthPlassAlgorithm.FindBreaks(items, wrapWidthTwips);
		var wrappedLines = new List<WrappedLine>(lineBreaks.Count);

		for (var lineIndex = 0; lineIndex < lineBreaks.Count; lineIndex++)
		{
			var isLastLine = lineIndex == lineBreaks.Count - 1;
			var isFirstLine = lineIndex == 0;
			var lineSegments = BuildWrappedLineSegments(plan, lineBreaks[lineIndex], wrapWidthTwips, alignment, indentation, isLastLine, isFirstLine);

			wrappedLines.Add(new WrappedLine(lineSegments));
		}

		return wrappedLines;
	}

	private static List<WrappedTextSegment> BuildWrappedLineSegments(WrapPlan plan, KnuthPlassLine line, float lineWidthTwips, ParagraphAlignment alignment, ParagraphIndentation indentation, bool isLastLine, bool isFirstLine)
	{
		var ratio = line.AdjustmentRatio;
		var effectiveAlignment = alignment == ParagraphAlignment.Justified && (isLastLine || ratio > MaxWordLikeJustificationRatio)
			? ParagraphAlignment.Left
			: alignment;

		var leftIndent = isFirstLine
			? indentation.GetFirstLineLeftIndent()
			: indentation.GetSubsequentLineLeftIndent();
		var effectiveLineWidth = lineWidthTwips - leftIndent - indentation.Right;
		if (effectiveLineWidth <= 0f)
		{
			effectiveLineWidth = 1f;
		}

		var contentWidth = ComputeLineContentWidth(plan.Items, line);
		var alignmentOffset = effectiveAlignment switch
		{
			ParagraphAlignment.Center => Math.Max(0f, (effectiveLineWidth - contentWidth) / 2f),
			ParagraphAlignment.Right => Math.Max(0f, effectiveLineWidth - contentWidth),
			_ => 0f,
		};

		return BuildLineSegmentsFromItems(plan, line, leftIndent + alignmentOffset, effectiveAlignment == ParagraphAlignment.Justified, ratio);
	}

	private static float ComputeLineContentWidth(IReadOnlyList<KnuthPlassItem> items, KnuthPlassLine line)
	{
		var contentWidth = 0f;
		for (var i = line.StartIndex; i < line.EndIndex && i < items.Count; i++)
		{
			contentWidth += items[i].Width;
		}

		if (line.EndIndex >= 0 && line.EndIndex < items.Count
			&& items[line.EndIndex] is KnuthPlassPenalty endPenalty)
		{
			contentWidth += endPenalty.Width;
		}

		return contentWidth;
	}

	private static List<WrappedTextSegment> BuildLineSegmentsFromItems(WrapPlan plan, KnuthPlassLine line, float startX, bool isJustified, float ratio)
	{
		var (tokens, items, itemTokenIndexes) = plan;
		var x = startX;
		var lineSegments = new List<WrappedTextSegment>();
		for (var i = line.StartIndex; i < line.EndIndex && i < items.Count; i++)
		{
			var tokenIndex = itemTokenIndexes[i];
			var itemWidth = items[i] switch
			{
				KnuthPlassBox box => box.Width,
				KnuthPlassGlue glue => ResolveGlueWidth(glue, isJustified, ratio),
				_ => (float?)null,
			};

			if (itemWidth is not { } width)
			{
				continue;
			}

			if (tokenIndex >= 0)
			{
				var token = tokens[tokenIndex];
				lineSegments.Add(new WrappedTextSegment(token.Text, x, width, token.Font, token.Brush, token.HyperlinkUri, token.HighlightFillColor));
			}

			x += width;
		}

		return lineSegments;
	}

	private static float ResolveGlueWidth(KnuthPlassGlue glue, bool isJustified, float ratio)
	{
		if (!isJustified)
		{
			return glue.Width;
		}

		return ratio >= 0f ? glue.Width + (ratio * glue.Stretch) : glue.Width + (ratio * glue.Shrink);
	}
	private static int CountWrappedLines(
		IReadOnlyList<TextSegment> segments,
		float lineWidthTwips,
		ParagraphAlignment alignment)
	{
		_ = alignment;

		if (segments.Count == 0 || lineWidthTwips <= 0f)
		{
			return 1;
		}

		var tokens = TokenizeWrappedSegments(segments);
		if (tokens.Count == 0)
		{
			return 1;
		}

		// Use a simple word-break simulation: accumulate token widths and break when the
		// effective wrap width (matching BuildWrappedLines' WordLikeWrapWidthRelaxation) is
		// exceeded. Non-whitespace tokens that exceed the line width on their own are placed
		// on their own line (Knuth-Plass overflow behaviour).
		var wrapWidthTwips = lineWidthTwips * WordLikeWrapWidthRelaxation;
		var lineCount = 1;
		var currentLineWidth = 0f;

		for (var i = 0; i < tokens.Count; i++)
		{
			var token = tokens[i];
			if (token.IsWhitespace)
			{
				currentLineWidth += token.WidthTwips;
				continue;
			}

			if (currentLineWidth + token.WidthTwips > wrapWidthTwips && currentLineWidth > 0f)
			{
				lineCount++;
				currentLineWidth = token.WidthTwips;
			}
			else
			{
				currentLineWidth += token.WidthTwips;
			}
		}

		return Math.Max(1, lineCount);
	}

	private static (List<KnuthPlassItem> Items, List<int> ItemTokenIndexes) BuildWrapItems(IReadOnlyList<WrappedToken> tokens)
	{
		var items = new List<KnuthPlassItem>(tokens.Count + 2);
		var itemTokenIndexes = new List<int>(tokens.Count + 2);
		for (var tokenIndex = 0; tokenIndex < tokens.Count; tokenIndex++)
		{
			var token = tokens[tokenIndex];
			if (token.IsWhitespace)
			{
				items.Add(new KnuthPlassGlue(
					token.WidthTwips,
					token.WidthTwips * DefaultWrapStretchRatio,
					token.WidthTwips * DefaultWrapShrinkRatio));
			}
			else
			{
				items.Add(new KnuthPlassBox(token.WidthTwips));
			}

			itemTokenIndexes.Add(tokenIndex);
		}

		items.Add(new KnuthPlassGlue(0f, float.PositiveInfinity, 0f));
		itemTokenIndexes.Add(-1);
		items.Add(new KnuthPlassPenalty(0f, KnuthPlassPenalty.NegativeInfinity));
		itemTokenIndexes.Add(-1);
		return (items, itemTokenIndexes);
	}

	private static IReadOnlyList<WrappedToken> TokenizeWrappedSegments(IReadOnlyList<TextSegment> segments)
	{
		var tokens = new List<WrappedToken>();
		var measurementEngine = new MeasurementEngine();
		for (var segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
		{
			var segment = segments[segmentIndex];
			var start = 0;
			while (start < segment.Text.Length)
			{
				var isWhitespace = char.IsWhiteSpace(segment.Text[start]);
				var end = start + 1;
				while (end < segment.Text.Length && char.IsWhiteSpace(segment.Text[end]) == isWhitespace)
				{
					end++;
				}

				var tokenText = segment.Text[start..end];
				tokens.Add(new WrappedToken(
					tokenText,
					MeasureWrappedTokenWidthTwips(tokenText, segment.Font, measurementEngine),
					segment.Font,
					segment.Brush,
					segment.HyperlinkUri,
					segment.HighlightFillColor,
					isWhitespace));
				start = end;
			}
		}

		return tokens;
	}

	private static float MeasureWrappedTokenWidthTwips(string text, RenderFont font, MeasurementEngine measurementEngine)
	{
		if (string.IsNullOrEmpty(text))
		{
			return 0f;
		}

		if (OperatingSystem.IsBrowser())
		{
			// Skia native font APIs are unavailable in WASM demo runtime.
			return EstimateTextWidthTwips(text, font.SizePoints);
		}

		using var typeface = SKTypeface.FromFamilyName(font.Family, FontStyleFromRenderFont(font)) ?? SKTypeface.Default;
		var advances = measurementEngine.MeasureGlyphAdvancesInTwips(typeface, font.SizePoints, text);
		var width = 0f;
		for (var i = 0; i < advances.Count; i++)
		{
			width += advances[i];
		}

		if (width <= 0f)
		{
			return EstimateTextWidthTwips(text, font.SizePoints);
		}

		return width;
	}

	private static SKFontStyle FontStyleFromRenderFont(RenderFont font)
	{
		return new SKFontStyle(
			font.IsBold ? (int)SKFontStyleWeight.Bold : (int)SKFontStyleWeight.Normal,
			(int)SKFontStyleWidth.Normal,
			font.IsItalic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);
	}

	private static int CountApproximateWrapTokens(IReadOnlyList<TextSegment> segments)
	{
		var tokenCount = 0;
		for (var segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
		{
			var text = segments[segmentIndex].Text;
			var start = 0;
			while (start < text.Length)
			{
				var isWhitespace = char.IsWhiteSpace(text[start]);
				var end = start + 1;
				while (end < text.Length && char.IsWhiteSpace(text[end]) == isWhitespace)
				{
					end++;
				}

				tokenCount++;
				if (tokenCount > MaxWrappedParagraphTokenCount)
				{
					return tokenCount;
				}

				start = end;
			}
		}

		return tokenCount;
	}

	private static float ComputeBaselineOffsetTwips(float fontSizePoints, float lineHeightTwips)
	{
		var ascentTwips = TwipConverter.PointsToTwips(fontSizePoints * BaselineAscentFactor);
		var maxBaselineTwips = lineHeightTwips * MaxBaselineLineHeightFactor;
		return MathF.Min(ascentTwips, maxBaselineTwips);
	}

	private static void NormalizeLightCellTextColor(IReadOnlyList<LayoutBlock> cellBlocks, ParagraphShading shading)
	{
		if (!IsLightBackground(shading.GetEffectiveBackgroundColor()))
		{
			return;
		}

		for (var i = 0; i < cellBlocks.Count; i++)
		{
			if (cellBlocks[i].Block is not ParagraphBlock paragraphBlock)
			{
				continue;
			}

			foreach (var run in paragraphBlock.SourceElement.Descendants<Run>())
			{
				var colorValue = run.RunProperties?.Color?.Val?.Value;
				if (!string.Equals(colorValue, "FFFFFF", StringComparison.OrdinalIgnoreCase)
					&& !string.Equals(colorValue, "WHITE", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				run.RunProperties ??= new RunProperties();
				run.RunProperties.Color ??= new Color();
				run.RunProperties.Color.Val = "000000";
			}
		}
	}

	private static bool IsLightBackground(string? backgroundColor)
	{
		if (!TryParseRenderColor(backgroundColor, out var color))
		{
			// Treat missing/unknown shading as a light background.
			return true;
		}

		var luminance = (0.2126f * color.R + 0.7152f * color.G + 0.0722f * color.B) / 255f;
		return luminance >= LightBackgroundLuminanceThreshold;
	}

	private static int CountVerticalSpan(ResolvedGridCell?[,] grid, int ownerRow, int ownerColumn, int rowCount)
	{
		var span = 1;
		for (var row = ownerRow + 1; row < rowCount; row++)
		{
			if (grid[row, ownerColumn] is { } cell
				&& cell.OwnerRowIndex == ownerRow
				&& cell.OwnerColumnIndex == ownerColumn)
			{
				span++;
			}
			else
			{
				break;
			}
		}

		return span;
	}

	private static void EmitTableBlock(TablePlaceholderBlock tableBlock, LayoutBlock layoutBlock, LayoutBlockPlacement placement, BlockEmitContext context)
	{
		var target = context.Target;
		var tableLayout = CreateRenderableTableLayout(tableBlock, placement.ContentWidthTwips, context.Styles);
		if (tableLayout is null)
		{
			var heightTwips = MathF.Max(layoutBlock.HeightTwips, 1f);
			target.DrawRect(new RenderRect(placement.XTwips, placement.YTwips, placement.ContentWidthTwips, heightTwips), null, context.DefaultStroke);
			return;
		}

		var frame = new TableEmitFrame(tableLayout, TableGridResolver.Resolve(tableLayout.Table), placement.XTwips + tableLayout.TableXOffset, placement.YTwips);
		EmitTableBackgrounds(frame, context);
		EmitTableBorders(frame, target);

		foreach (var position in TableLayoutEngine.ComputeCellPositions(tableLayout))
		{
			EmitTableCell(position, frame, placement, context);
		}
	}

	private static void EmitTableBackgrounds(TableEmitFrame frame, BlockEmitContext context)
	{
		foreach (var background in TableLayoutEngine.ComputeCellBackgrounds(frame.Layout, context.Styles))
		{
			if (!TryParseRenderColor(background.Shading.GetEffectiveBackgroundColor(), out var fillColor))
			{
				continue;
			}

			context.Target.DrawRect(
				new RenderRect(frame.X + background.X, frame.Y + background.Y, background.Width, background.Height),
				new SolidRenderBrush(fillColor),
				null);
		}
	}

	private static void EmitTableBorders(TableEmitFrame frame, IRenderTarget target)
	{
		foreach (var segment in TableLayoutEngine.ComputeBorderSegments(frame.Layout))
		{
			if (segment.WidthTwips <= 0f || !TryParseRenderColor(segment.ColorHex, out var strokeColor))
			{
				continue;
			}

			target.DrawLine(
				new RenderPoint(frame.X + segment.X1, frame.Y + segment.Y1),
				new RenderPoint(frame.X + segment.X2, frame.Y + segment.Y2),
				new RenderStroke(strokeColor, segment.WidthTwips));
		}
	}

	private static void EmitTableCell(CellPosition position, TableEmitFrame frame, LayoutBlockPlacement placement, BlockEmitContext context)
	{
		var table = frame.Layout.Table;
		var target = context.Target;
		var rowCount = frame.Grid.GetLength(0);
		var columnCount = frame.Grid.GetLength(1);
		var contentWidth = TableLayoutEngine.ComputeContentWidth(position.Width, position.Cell.Margins, table.BorderSpacingTwips);
		if (contentWidth <= 0f)
		{
			return;
		}

		var (cellBlocks, totalHeight) = TableLayoutEngine.LayoutCellContent(position.Cell, table.BorderSpacingTwips, contentWidth);
		if (cellBlocks.Count == 0)
		{
			return;
		}

		var rowSpan = CountVerticalSpan(frame.Grid, position.RowIndex, position.ColumnIndex, rowCount);
		var remainingColumns = Math.Max(0, columnCount - position.ColumnIndex);
		var columnSpan = Math.Min(position.Cell.GridSpan, remainingColumns);

		var effectiveShading = position.Cell.Shading.HasVisibleShading
			? position.Cell.Shading
			: TableStyleResolver.ResolveCellShading(
				context.Styles,
				table,
				position.RowIndex,
				position.ColumnIndex,
				rowSpan,
				columnSpan,
				rowCount,
				columnCount);
		NormalizeLightCellTextColor(cellBlocks, effectiveShading);

		var verticalOffset = TableLayoutEngine.ComputeVerticalContentOffset(position.Height, totalHeight, position.Cell.VerticalAlignment);
		var contentX = frame.X + position.X + position.Cell.Margins.Left + table.BorderSpacingTwips;
		var currentY = frame.Y + position.Y + verticalOffset + position.Cell.Margins.Top + table.BorderSpacingTwips;

		target.PushClip(new RenderRect(frame.X + position.X, frame.Y + position.Y, position.Width, position.Height));
		foreach (var cellBlock in cellBlocks)
		{
			EmitLayoutBlock(cellBlock, new LayoutBlockPlacement(cellBlock, contentX, currentY, contentWidth, placement.ColumnIndex), context);
			currentY += cellBlock.HeightTwips;
		}

		target.PopClip();
	}
	private static TableLayoutResult? CreateRenderableTableLayout(TablePlaceholderBlock tableBlock, float availableWidthTwips, Styles? styles = null)
	{
		var parsedTable = TableParser.Parse(tableBlock.TableElement, styles);
		if (parsedTable.Rows.Count == 0)
		{
			return null;
		}

		// Resolve alignment from table style when not set via direct formatting.
		var effectiveAlignment = parsedTable.Alignment;
		if (effectiveAlignment == TableAlignment.Left && parsedTable.StyleId is not null && styles is not null)
		{
			effectiveAlignment = ResolveTableStyleAlignment(parsedTable.StyleId, styles);
		}

		var fullAvailableWidth = Math.Max(0f, availableWidthTwips - parsedTable.IndentationTwips);
		var effectiveAvailableWidth = fullAvailableWidth;

		// Honour explicit table width (tblW) when specified.
		if (parsedTable.Width.Type == TableWidthUnit.Dxa && parsedTable.Width.Value > 0f)
		{
			effectiveAvailableWidth = Math.Min(effectiveAvailableWidth, parsedTable.Width.Value);
		}
		else if (parsedTable.Width.Type == TableWidthUnit.Pct && parsedTable.Width.Value > 0f)
		{
			// Value is in fiftieths of a percent (5000 = 100%).
			effectiveAvailableWidth = Math.Min(effectiveAvailableWidth, effectiveAvailableWidth * parsedTable.Width.Value / 5000f);
		}

		var fixedLayout = TableLayoutEngine.Layout(parsedTable, effectiveAvailableWidth);
		if (fixedLayout.ColumnWidths.Count == 0 || fixedLayout.TableWidthTwips <= 0f)
		{
			var autoFitLayout = TableLayoutEngine.LayoutAutoFit(parsedTable, effectiveAvailableWidth);
			return autoFitLayout.ColumnWidths.Count == 0 || autoFitLayout.TableWidthTwips <= 0f
				? null
				: autoFitLayout;
		}

		var rowHeights = TableLayoutEngine.ComputeRowHeights(parsedTable, fixedLayout.ColumnWidths);
		var totalHeight = 0f;
		foreach (var rowHeight in rowHeights)
		{
			totalHeight += rowHeight;
		}

		// Recompute alignment offset using the full available width (before tblW capping)
		// so centered/right-aligned tables are positioned relative to the page content area.
		var tableWidth = SumColumnWidths(fixedLayout.ColumnWidths);
		var alignmentOffset = effectiveAlignment switch
		{
			TableAlignment.Center => Math.Max(0f, (fullAvailableWidth - tableWidth) / 2f),
			TableAlignment.Right => Math.Max(0f, fullAvailableWidth - tableWidth),
			_ => parsedTable.IndentationTwips > 0f ? 0f : fixedLayout.TableXOffset,
		};

		return new TableLayoutResult
		{
			TableXOffset = alignmentOffset,
			TableWidthTwips = fixedLayout.TableWidthTwips,
			ColumnOffsets = fixedLayout.ColumnOffsets,
			ColumnWidths = fixedLayout.ColumnWidths,
			RowHeights = rowHeights,
			TotalHeightTwips = totalHeight,
			Table = parsedTable,
		};
	}

	private static float SumColumnWidths(IReadOnlyList<float> columnWidths)
	{
		var total = 0f;
		for (var i = 0; i < columnWidths.Count; i++)
		{
			total += columnWidths[i];
		}

		return total;
	}

	private static TableAlignment ResolveTableStyleAlignment(string styleId, Styles styles)
	{
		var visited = new HashSet<string>(StringComparer.Ordinal);
		var currentId = styleId;

		while (!string.IsNullOrEmpty(currentId) && visited.Add(currentId))
		{
			var style = styles.Elements<Style>()
				.FirstOrDefault(s => s.StyleId?.Value == currentId && s.Type?.Value == StyleValues.Table);
			if (style is null)
			{
				break;
			}

			var jc = style.StyleTableProperties?.TableJustification;
			if (jc?.Val?.Value is not null)
			{
				if (jc.Val.Value == TableRowAlignmentValues.Center)
				{
					return TableAlignment.Center;
				}

				if (jc.Val.Value == TableRowAlignmentValues.Right)
				{
					return TableAlignment.Right;
				}

				return TableAlignment.Left;
			}

			currentId = style.BasedOn?.Val?.Value;
		}

		return TableAlignment.Left;
	}

	private static ParagraphShading ResolveParagraphShading(Styles? styles, string? styleId, Paragraph paragraph)
	{
		// Direct paragraph shading takes precedence over style-inherited shading.
		var directShd = paragraph.ParagraphProperties?.Shading;
		if (directShd is not null)
		{
			var direct = TableParser.ParseShading(directShd);
			if (direct.HasVisibleShading)
			{
				return direct;
			}
		}

		if (styles is null || styleId is null)
		{
			return ParagraphShading.None;
		}

		// Walk the paragraph style inheritance chain to find shading.
		var currentId = styleId;
		var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		while (!string.IsNullOrEmpty(currentId) && visited.Add(currentId))
		{
			var style = styles.Elements<Style>()
				.FirstOrDefault(s => s.Type?.Value == StyleValues.Paragraph && s.StyleId?.Value == currentId);
			if (style is null)
			{
				break;
			}

			var shd = style.StyleParagraphProperties?.GetFirstChild<Shading>();
			if (shd is not null)
			{
				var styleShading = TableParser.ParseShading(shd);
				if (styleShading.HasVisibleShading)
				{
					return styleShading;
				}
			}

			currentId = style.BasedOn?.Val?.Value;
		}

		return ParagraphShading.None;
	}

	private static void EmitParagraphBorders(ParagraphBorders borders, float xTwips, float yTopTwips, float heightTwips, float widthTwips, IRenderTarget target)
	{
		if (borders.Bottom is { IsVisible: true } bottom)
		{
			var strokeColor = TryParseRenderColor(bottom.Color, out var color) ? color : DefaultTextColor;
			var strokeWidth = Math.Max(bottom.GetWidthTwips(), 1f);
			var y = yTopTwips + heightTwips + bottom.GetSpacingTwips();
			target.DrawLine(new RenderPoint(xTwips, y), new RenderPoint(xTwips + widthTwips, y), new RenderStroke(strokeColor, strokeWidth));
		}

		if (borders.Top is { IsVisible: true } top)
		{
			var strokeColor = TryParseRenderColor(top.Color, out var color) ? color : DefaultTextColor;
			var strokeWidth = Math.Max(top.GetWidthTwips(), 1f);
			var y = yTopTwips - top.GetSpacingTwips();
			target.DrawLine(new RenderPoint(xTwips, y), new RenderPoint(xTwips + widthTwips, y), new RenderStroke(strokeColor, strokeWidth));
		}
	}

	private static NumberingLevelStyle ResolveListStyle(RenderOptions options, int numberingId, int numberingLevel)
	{
		var canonicalId = options.NumberingIdNormalization.TryGetValue(numberingId, out var cid) ? cid : numberingId;
		var styleKey = CreateNumberingStyleKey(canonicalId, numberingLevel);
		if (options.NumberingStyles.TryGetValue(styleKey, out var configuredStyle))
		{
			return configuredStyle;
		}

		return new NumberingLevelStyle
		{
			LevelIndex = numberingLevel,
			Start = 1,
			NumberFormat = "decimal",
			LevelText = BuildDefaultLevelPattern(numberingLevel)
		};
	}

	private static string CreateNumberingStyleKey(int numberingId, int numberingLevel)
	{
		return $"{numberingId}:{numberingLevel}";
	}

	private static string BuildDefaultLevelPattern(int numberingLevel)
	{
		if (numberingLevel < 0)
		{
			return "%1.";
		}

		var parts = new List<string>(numberingLevel + 1);
		for (var index = 1; index <= numberingLevel + 1; index++)
		{
			parts.Add($"%{index}");
		}

		return string.Join(".", parts) + ".";
	}

	private static IReadOnlyList<TextSegment> BuildTextSegments(Paragraph paragraph, RenderFont defaultFont, string defaultFamily, int currentPageNumber, int totalPageCount, DateTime renderTimestampUtc, OpenXmlPart? part = null)
	{
		var segments = new List<TextSegment>();
		var context = new SegmentBuildContext(segments, defaultFont, defaultFamily, new Stack<ActiveField>(), currentPageNumber, totalPageCount, renderTimestampUtc, part);
		foreach (var child in paragraph.ChildElements)
		{
			AppendSegmentsFromElement(child, context);
		}

		if (segments.Count == 0)
		{
			var text = paragraph.InnerText;
			if (!string.IsNullOrWhiteSpace(text))
			{
				segments.Add(new TextSegment(text, defaultFont, new SolidRenderBrush(DefaultTextColor), null, null));
			}
		}

		return segments;
	}

	private static void AppendSegmentsFromElement(OpenXmlElement element, SegmentBuildContext context, string? hyperlinkUri = null)
	{
		switch (element)
		{
			case Run run:
				AppendSegmentsFromRun(run, context, hyperlinkUri);
				break;
			case SimpleField simpleField:
				AppendSegmentsFromSimpleField(simpleField, context.Segments, context.DefaultFont, context.DefaultFamily, context.CurrentPageNumber, context.TotalPageCount, context.RenderTimestampUtc);
				break;
			case Hyperlink hyperlink:
				var resolvedUri = RunElementParser.ResolveHyperlinkUri(hyperlink, context.Part);
				foreach (var child in hyperlink.ChildElements)
				{
					AppendSegmentsFromElement(child, context, resolvedUri);
				}

				break;
			default:
				foreach (var child in element.ChildElements)
				{
					AppendSegmentsFromElement(child, context, hyperlinkUri);
				}
				break;
		}
	}

	private static void AppendSegmentsFromRun(Run run, SegmentBuildContext context, string? hyperlinkUri = null)
	{
		var segments = context.Segments;
		var activeFields = context.ActiveFields;
		var runProperties = run.RunProperties;
		var font = ResolveRunFont(runProperties, context.DefaultFont, context.DefaultFamily);
		var brush = ResolveRunBrush(runProperties);
		var highlightFillColor = ResolveRunHighlightFillColor(runProperties);
		var isRtl = IsOn(runProperties?.RightToLeftText);
		var style = new RunStyle(font, brush, highlightFillColor, hyperlinkUri, isRtl);

		foreach (var fieldCode in run.Elements<FieldCode>().Where(_ => activeFields.Count > 0))
		{
			activeFields.Peek().InstructionBuilder.Append(fieldCode.Text);
		}

		foreach (var fieldChar in run.Elements<FieldChar>())
		{
			HandleFieldChar(fieldChar, activeFields, font, brush, highlightFillColor);
		}

		// Process run children in document order to capture tabs interspersed with text
		var textBuilder = new StringBuilder();
		foreach (var child in run.ChildElements)
		{
			if (child is Text t)
			{
				textBuilder.Append(t.Text);
			}
			else if (child is TabChar)
			{
				FlushRunText(context, textBuilder, style);
				segments.Add(new TextSegment("\t", font, brush, null, highlightFillColor, IsTab: true));
			}
			else if (child is PositionalTab pTab)
			{
				FlushRunText(context, textBuilder, style);

				// w:ptab with relativeTo="margin" and alignment="right" snaps to the right margin.
				var isPtabRight = pTab.Alignment?.Value == AbsolutePositionTabAlignmentValues.Right;
				segments.Add(new TextSegment("\t", font, brush, null, highlightFillColor, IsTab: true, IsRtl: isRtl, IsPtabRightMargin: isPtabRight));
			}
		}

		FlushRunText(context, textBuilder, style);
	}

	private static void FlushRunText(SegmentBuildContext context, StringBuilder textBuilder, RunStyle style)
	{
		if (textBuilder.Length == 0)
		{
			return;
		}

		RouteTextToSegment(context, textBuilder.ToString(), style);
		textBuilder.Clear();
	}

	private static void RouteTextToSegment(SegmentBuildContext context, string text, RunStyle style)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		var segments = context.Segments;
		if (context.ActiveFields.Count == 0)
		{
			AppendTextSegment(segments, text, style.Font, style.Brush, style.HighlightFillColor, style.HyperlinkUri, style.IsRtl);
			return;
		}

		var activeField = context.ActiveFields.Peek();
		if (!activeField.IsResultSection)
		{
			return;
		}

		if (activeField.Kind is FieldKind.Page or FieldKind.NumPages or FieldKind.Date or FieldKind.Time)
		{
			AppendComputedFieldValue(context, activeField, style);
			return;
		}

		AppendTextSegment(segments, text, style.Font, style.Brush, style.HighlightFillColor, activeField.HyperlinkUri, style.IsRtl);
	}

	private static void AppendComputedFieldValue(SegmentBuildContext context, ActiveField activeField, RunStyle style)
	{
		if (activeField.HasRenderedComputedValue)
		{
			return;
		}

		var fieldFont = activeField.BeginFont ?? style.Font;
		var fieldBrush = activeField.BeginBrush ?? style.Brush;
		var fieldHighlightFillColor = activeField.BeginHighlightFillColor ?? style.HighlightFillColor;
		var value = ComputeFieldValue(activeField.Kind, context.CurrentPageNumber, context.TotalPageCount, context.RenderTimestampUtc, activeField.InstructionBuilder.ToString());
		AppendTextSegment(context.Segments, value, fieldFont, fieldBrush, fieldHighlightFillColor, null, style.IsRtl);
		activeField.HasRenderedComputedValue = true;
	}
	private static void AppendSegmentsFromSimpleField(SimpleField simpleField, List<TextSegment> segments, RenderFont defaultFont, string defaultFamily, int currentPageNumber, int totalPageCount, DateTime renderTimestampUtc)
	{
		var instructionText = simpleField.Instruction?.Value;
		var hyperlinkUri = ExtractHyperlinkUri(instructionText);
		var kind = ParseFieldKind(simpleField.Instruction?.Value);
		var firstRunProperties = simpleField.Descendants<Run>().Select(r => r.RunProperties).FirstOrDefault(r => r is not null);
		var brush = ResolveRunBrush(firstRunProperties);
		var highlightFillColor = ResolveRunHighlightFillColor(firstRunProperties);
		if (kind is FieldKind.Page or FieldKind.NumPages or FieldKind.Date or FieldKind.Time)
		{
			AppendTextSegment(segments, ComputeFieldValue(kind, currentPageNumber, totalPageCount, renderTimestampUtc, instructionText), defaultFont, brush, highlightFillColor, null);
			return;
		}

		var text = string.Concat(simpleField.Descendants<Text>().Select(t => t.Text));
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}

		var font = ResolveRunFont(firstRunProperties, defaultFont, defaultFamily);
		AppendTextSegment(segments, text, font, brush, highlightFillColor, hyperlinkUri);
	}

	private static void HandleFieldChar(FieldChar fieldChar, Stack<ActiveField> activeFields, RenderFont? beginFont = null, RenderBrush? beginBrush = null, RenderColor? beginHighlightFillColor = null)
	{
		if (fieldChar.FieldCharType is null)
		{
			return;
		}

		if (fieldChar.FieldCharType == FieldCharValues.Begin)
		{
			activeFields.Push(new ActiveField { BeginFont = beginFont, BeginBrush = beginBrush, BeginHighlightFillColor = beginHighlightFillColor });
			return;
		}

		if (fieldChar.FieldCharType == FieldCharValues.Separate)
		{
			if (activeFields.Count == 0)
			{
				return;
			}

			var activeField = activeFields.Peek();
			activeField.IsResultSection = true;
			var instructionText = activeField.InstructionBuilder.ToString();
			activeField.Kind = ParseFieldKind(instructionText);
			activeField.HyperlinkUri = activeField.Kind == FieldKind.Hyperlink ? ExtractHyperlinkUri(instructionText) : null;
			return;
		}

		if (fieldChar.FieldCharType == FieldCharValues.End && activeFields.Count > 0)
		{
			activeFields.Pop();
		}
	}

	private static RenderFont ResolveRunFont(RunProperties? runProperties, RenderFont defaultFont, string defaultFamily)
	{
		var fontFamily = runProperties?.RunFonts?.Ascii?.Value;
		var fontSizePoints = defaultFont.SizePoints;
		var fontSizeValue = runProperties?.FontSize?.Val?.Value;
		if (int.TryParse(fontSizeValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sizeHalfPoints))
		{
			fontSizePoints = sizeHalfPoints / 2f;
		}

		return new RenderFont(
			string.IsNullOrWhiteSpace(fontFamily) ? defaultFamily : fontFamily,
			fontSizePoints,
			IsOn(runProperties?.Bold),
			IsOn(runProperties?.Italic),
			IsUnderline(runProperties?.Underline),
			IsOn(runProperties?.Strike));
	}

	private static void AppendTextSegment(List<TextSegment> segments, string text, RenderFont font, RenderBrush brush, RenderColor? highlightFillColor, string? hyperlinkUri, bool isRtl = false)
	{
		if (segments.Count > 0
			&& !segments[^1].IsTab
			&& segments[^1].IsRtl == isRtl
			&& segments[^1].Font == font
			&& segments[^1].Brush == brush
			&& segments[^1].HighlightFillColor == highlightFillColor
			&& string.Equals(segments[^1].HyperlinkUri, hyperlinkUri, StringComparison.Ordinal))
		{
			segments[^1] = segments[^1] with { Text = segments[^1].Text + text };
		}
		else
		{
			segments.Add(new TextSegment(text, font, brush, hyperlinkUri, highlightFillColor, IsRtl: isRtl));
		}
	}

	private static readonly (HighlightColorValues Value, HighlightColor Color)[] HighlightColorTable =
	[
		(HighlightColorValues.Black, HighlightColor.Black),
		(HighlightColorValues.Blue, HighlightColor.Blue),
		(HighlightColorValues.Cyan, HighlightColor.Cyan),
		(HighlightColorValues.DarkBlue, HighlightColor.DarkBlue),
		(HighlightColorValues.DarkCyan, HighlightColor.DarkCyan),
		(HighlightColorValues.DarkGray, HighlightColor.DarkGray),
		(HighlightColorValues.DarkGreen, HighlightColor.DarkGreen),
		(HighlightColorValues.DarkMagenta, HighlightColor.DarkMagenta),
		(HighlightColorValues.DarkRed, HighlightColor.DarkRed),
		(HighlightColorValues.DarkYellow, HighlightColor.DarkYellow),
		(HighlightColorValues.Green, HighlightColor.Green),
		(HighlightColorValues.LightGray, HighlightColor.LightGray),
		(HighlightColorValues.Magenta, HighlightColor.Magenta),
		(HighlightColorValues.Red, HighlightColor.Red),
		(HighlightColorValues.White, HighlightColor.White),
		(HighlightColorValues.Yellow, HighlightColor.Yellow),
	];

	private static RenderColor? ResolveRunHighlightFillColor(RunProperties? runProperties)
	{
		var highlightColor = ResolveHighlightColor(runProperties?.Highlight?.Val?.Value);
		if (highlightColor == HighlightColor.None)
		{
			return null;
		}

		var highlightHex = HighlightColorMap.ToHexRgb(highlightColor);
		return TryParseRenderColor(highlightHex, out var highlightFillColor) ? highlightFillColor : null;
	}

	private static HighlightColor ResolveHighlightColor(HighlightColorValues? highlightValue)
	{
		foreach (var (value, color) in HighlightColorTable)
		{
			if (highlightValue == value)
			{
				return color;
			}
		}

		return HighlightColor.None;
	}

	private static RenderBrush ResolveRunBrush(RunProperties? runProperties)
	{
		return TryParseRenderColor(runProperties?.Color?.Val?.Value, out var color)
			? new SolidRenderBrush(color)
			: new SolidRenderBrush(DefaultTextColor);
	}

	private static bool TryParseRenderColor(string? colorValue, out RenderColor color)
	{
		if (string.IsNullOrWhiteSpace(colorValue))
		{
			color = default;
			return false;
		}

		if (string.Equals(colorValue, "auto", StringComparison.OrdinalIgnoreCase))
		{
			color = DefaultTextColor;
			return true;
		}

		if (colorValue.Length == 6
			&& byte.TryParse(colorValue.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r)
			&& byte.TryParse(colorValue.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g)
			&& byte.TryParse(colorValue.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
		{
			color = new RenderColor(r, g, b);
			return true;
		}

		color = colorValue.ToUpperInvariant() switch
		{
			"RED" => new RenderColor(255, 0, 0),
			"BLUE" => new RenderColor(0, 0, 255),
			"GREEN" => new RenderColor(0, 128, 0),
			"BLACK" => new RenderColor(0, 0, 0),
			"WHITE" => new RenderColor(255, 255, 255),
			_ => default,
		};

		return color != default;
	}

	private static string? ExtractHyperlinkUri(string? instruction)
	{
		if (string.IsNullOrWhiteSpace(instruction))
		{
			return null;
		}

		var index = instruction.IndexOf("HYPERLINK", StringComparison.OrdinalIgnoreCase);
		if (index < 0)
		{
			return null;
		}

		var remaining = instruction[(index + "HYPERLINK".Length)..].Trim();
		if (remaining.Length == 0)
		{
			return null;
		}

		if (remaining[0] == '"')
		{
			var endQuote = remaining.IndexOf('"', 1);
			if (endQuote > 1)
			{
				return remaining[1..endQuote];
			}

			return null;
		}

		var token = remaining.Split([' ', '\\', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)[0];
		return token.Length == 0 ? null : token;
	}

	private static FieldKind ParseFieldKind(string? instruction)
	{
		if (string.IsNullOrWhiteSpace(instruction))
		{
			return FieldKind.Other;
		}

		var trimmed = instruction.Trim();
		var firstToken = trimmed.Split([' ', '\\', '"', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)[0].ToUpperInvariant();
		return firstToken switch
		{
			"PAGE" => FieldKind.Page,
			"NUMPAGES" => FieldKind.NumPages,
			"DATE" => FieldKind.Date,
			"TIME" => FieldKind.Time,
			"TOC" => FieldKind.Toc,
			"HYPERLINK" => FieldKind.Hyperlink,
			"REF" => FieldKind.Ref,
			"PAGEREF" => FieldKind.PageRef,
			"IF" => FieldKind.If,
			"MERGEFIELD" => FieldKind.MergeField,
			_ => FieldKind.Other
		};
	}

	private static string ComputeFieldValue(FieldKind fieldKind, int currentPageNumber, int totalPageCount, DateTime renderTimestampUtc, string? instruction = null)
	{
		return fieldKind switch
		{
			FieldKind.Page => currentPageNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
			FieldKind.NumPages => totalPageCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
			FieldKind.Date => FormatDateTimeField(renderTimestampUtc, instruction, "d"),
			FieldKind.Time => FormatDateTimeField(renderTimestampUtc, instruction, "T"),
			_ => string.Empty
		};
	}

	/// <summary>
	/// Formats a DATE or TIME field using the OOXML \@ picture switch when present.
	/// Falls back to <paramref name="fallbackFormat"/> when no switch is found.
	/// </summary>
	private static string FormatDateTimeField(DateTime dt, string? instruction, string fallbackFormat)
	{
		var picture = TryExtractDatePictureSwitch(instruction);
		if (picture is null)
		{
			return dt.ToString(fallbackFormat, CultureInfo.InvariantCulture);
		}

		var dotNetFormat = ConvertOoxmlDatePictureToDotNet(picture);
		return dt.ToString(dotNetFormat, CultureInfo.InvariantCulture);
	}

	/// <summary>
	/// Extracts the quoted format string from an OOXML \@ date-picture switch,
	/// e.g. <c>DATE \@ "yyyy"</c> → <c>yyyy</c>.
	/// Returns <see langword="null"/> when no switch is present.
	/// </summary>
	private static string? TryExtractDatePictureSwitch(string? instruction)
	{
		if (string.IsNullOrWhiteSpace(instruction))
		{
			return null;
		}

		// Match \@ "<picture>" (with optional extra whitespace)
		var idx = instruction.IndexOf(@"\@", StringComparison.Ordinal);
		if (idx < 0)
		{
			return null;
		}

		var after = instruction.AsSpan(idx + 2).TrimStart();
		if (after.IsEmpty || after[0] != '"')
		{
			return null;
		}

		var closeQuote = after[1..].IndexOf('"');
		if (closeQuote < 0)
		{
			return null;
		}

		return after.Slice(1, closeQuote).ToString();
	}

	/// <summary>
	/// Converts an OOXML date-picture string to a .NET <see cref="DateTime.ToString(string)"/> format string.
	/// OOXML uses the same tokens as Word's field formatting: d/dd/ddd/dddd, M/MM/MMM/MMMM, yy/yyyy, h/hh/H/HH, m/mm, s/ss, AM/PM.
	/// </summary>
	private static string ConvertOoxmlDatePictureToDotNet(string picture)
	{
		// OOXML picture codes map directly to .NET format codes with minor differences.
		// The main difference: OOXML uses single 'y' for 2-digit year and 'yyyy' for 4-digit.
		// .NET uses 'yy' for 2-digit year; a single 'y' means 1-or-2-digit.
		// We normalise lone 'y'→'yy' so that the .NET result matches Word's output.
		if (picture == "y")
		{
			return "yy";
		}

		// All other OOXML date picture tokens (d, dd, ddd, dddd, M, MM, MMM, MMMM, yy, yyyy,
		// h, hh, H, HH, m, mm, s, ss) are identical in .NET.
		return picture;
	}

	private static bool IsOn(OnOffType? property)
	{
		if (property is null)
		{
			return false;
		}

		return property.Val is null || property.Val.Value;
	}

	private static bool IsUnderline(Underline? underline)
	{
		if (underline is null)
		{
			return false;
		}

		return underline.Val is null || underline.Val.Value != UnderlineValues.None;
	}

	private static float EstimateTextWidthTwips(string text, float sizePoints)
	{
		return text.Length * sizePoints * AverageGlyphWidthFactor;
	}

	private static float ComputeTotalSegmentWidth(IReadOnlyList<TextSegment> segments)
	{
		var total = 0f;
		for (var i = 0; i < segments.Count; i++)
		{
			if (!segments[i].IsTab)
			{
				total += EstimateTextWidthTwips(segments[i].Text, segments[i].Font.SizePoints);
			}
		}

		return total;
	}

	private static float EstimateTextHeightTwips(float sizePoints)
	{
		return MathF.Max(TwipConverter.PointsToTwips(sizePoints) * 1.2f, 1f);
	}
	private static float EstimateHighlightTopOffsetTwips(float sizePoints)
	{
		return MathF.Max(TwipConverter.PointsToTwips(sizePoints * BaselineAscentFactor), 1f);
	}

	private static float EstimateHighlightHeightTwips(float sizePoints)
	{
		return MathF.Max(TwipConverter.PointsToTwips(sizePoints), 1f);
	}

	private static string GetTextAfterTab(IReadOnlyList<TextSegment> segments, int tabIndex)
	{
		var builder = new StringBuilder();
		for (var i = tabIndex + 1; i < segments.Count; i++)
		{
			if (segments[i].IsTab)
			{
				break;
			}

			builder.Append(segments[i].Text);
		}

		return builder.ToString();
	}

	private sealed class ActiveField
	{
		public StringBuilder InstructionBuilder { get; } = new();

		public bool IsResultSection { get; set; }

		public FieldKind Kind { get; set; } = FieldKind.Other;

		public string? HyperlinkUri { get; set; }

		public bool HasRenderedComputedValue { get; set; }

		/// <summary>
		/// Font from the field begin run, used to format computed field values.
		/// </summary>
		public RenderFont? BeginFont { get; set; }

		/// <summary>
		/// Brush from the field begin run, used to format computed field values.
		/// </summary>
		public RenderBrush? BeginBrush { get; set; }

		/// <summary>
		/// Highlight fill from the field begin run, used to format computed field values.
		/// </summary>
		public RenderColor? BeginHighlightFillColor { get; set; }
	}

	private enum FieldKind
	{
		Other,
		Page,
		NumPages,
		Date,
		Time,
		Toc,
		Hyperlink,
		Ref,
		PageRef,
		If,
		MergeField
	}

	private readonly record struct TextSegment(string Text, RenderFont Font, RenderBrush Brush, string? HyperlinkUri, RenderColor? HighlightFillColor = null, bool IsTab = false, bool IsRtl = false, bool IsPtabRightMargin = false);
	private readonly record struct WrappedToken(string Text, float WidthTwips, RenderFont Font, RenderBrush Brush, string? HyperlinkUri, RenderColor? HighlightFillColor, bool IsWhitespace);
	private readonly record struct WrappedTextSegment(string Text, float XOffset, float WidthTwips, RenderFont Font, RenderBrush Brush, string? HyperlinkUri, RenderColor? HighlightFillColor);

	private readonly record struct TextDrawing(string Text, RenderFont Font, RenderBrush Brush, RenderColor? HighlightFillColor, string? HyperlinkUri);

	private sealed record TableEmitFrame(TableLayoutResult Layout, ResolvedGridCell?[,] Grid, float X, float Y);

	private readonly record struct RunStyle(RenderFont Font, RenderBrush Brush, RenderColor? HighlightFillColor, string? HyperlinkUri, bool IsRtl);

	private readonly record struct ImageLayoutItem(bool IsTab, bool IsImage, bool IsPtabRightMargin, float Width, Drawing? Drawing);

	private sealed record WrapPlan(IReadOnlyList<WrappedToken> Tokens, IReadOnlyList<KnuthPlassItem> Items, IReadOnlyList<int> ItemTokenIndexes);

	private sealed record BlockEmitContext(
		IRenderTarget Target,
		RenderOptions RenderOptions,
		RenderFont DefaultFont,
		string FontFamily,
		RenderBrush DefaultBrush,
		RenderStroke DefaultStroke,
		int CurrentPageNumber,
		int TotalPageCount,
		DateTime RenderTimestampUtc,
		ListNumberingState ListState,
		SectionInfo? Section,
		IReadOnlyDictionary<string, ImageData>? Images,
		Styles? Styles);

	private sealed record SegmentBuildContext(
		List<TextSegment> Segments,
		RenderFont DefaultFont,
		string DefaultFamily,
		Stack<ActiveField> ActiveFields,
		int CurrentPageNumber,
		int TotalPageCount,
		DateTime RenderTimestampUtc,
		OpenXmlPart? Part);
	private readonly record struct WrappedLine(IReadOnlyList<WrappedTextSegment> Segments);

	private static void EmitBarTabStops(ParagraphBlock paragraphBlock, LayoutBlockPlacement placement, float yTwips, float heightTwips, IRenderTarget target)
	{
		var tabProfile = TabStopParser.ParseTabStops(paragraphBlock.SourceElement.ParagraphProperties);
		foreach (var stop in tabProfile.ExplicitStops)
		{
			if (stop.Type != TabStopType.Bar)
			{
				continue;
			}

			var barX = placement.XTwips + stop.PositionTwips;
			target.DrawLine(
				new RenderPoint(barX, yTwips),
				new RenderPoint(barX, yTwips + heightTwips),
				BarTabStroke);
		}
	}

	private static void EmitParagraphImages(
		ParagraphBlock paragraphBlock,
		LayoutBlockPlacement placement,
		IRenderTarget target,
		RenderFont defaultFont,
		SectionInfo? section,
		IReadOnlyDictionary<string, ImageData> images)
	{
		// Items are collected first so tab stops (including right-aligned tabs that push inline
		// images to the right) resolve before any draw commands are emitted.
		var items = CollectImageLayoutItems(paragraphBlock, defaultFont);
		var currentX = ResolveImageStartX(paragraphBlock, placement, items);
		var currentY = placement.YTwips;
		var tabProfile = TabStopParser.ParseTabStops(paragraphBlock.SourceElement.ParagraphProperties);

		for (var itemIndex = 0; itemIndex < items.Count; itemIndex++)
		{
			var item = items[itemIndex];
			if (item.IsTab)
			{
				currentX = ResolveImageTabX(items, itemIndex, tabProfile, placement, currentX);
				continue;
			}

			if (item.IsImage && item.Drawing is not null)
			{
				EmitDrawingImage(item.Drawing, target, section, images, placement, currentX, currentY);
			}

			currentX += item.Width;
		}
	}

	private static List<ImageLayoutItem> CollectImageLayoutItems(ParagraphBlock paragraphBlock, RenderFont defaultFont)
	{
		var items = new List<ImageLayoutItem>();
		foreach (var run in paragraphBlock.SourceElement.Descendants<Run>())
		{
			foreach (var child in run.ChildElements)
			{
				if (child is Text t)
				{
					items.Add(new ImageLayoutItem(false, false, false, EstimateTextWidthTwips(t.Text, defaultFont.SizePoints), null));
				}
				else if (child is TabChar)
				{
					items.Add(new ImageLayoutItem(true, false, false, 0f, null));
				}
				else if (child is PositionalTab pTab)
				{
					var isPtabRight = pTab.Alignment?.Value == AbsolutePositionTabAlignmentValues.Right;
					items.Add(new ImageLayoutItem(true, false, isPtabRight, 0f, null));
				}
				else if (child is Drawing drawing)
				{
					var inlineExt = drawing.GetFirstChild<DW.Inline>()?.Extent;
					var width = inlineExt is not null ? TwipConverter.EmusToTwips(inlineExt.Cx ?? 0) : 0f;
					items.Add(new ImageLayoutItem(false, true, false, width, drawing));
				}
			}
		}

		return items;
	}

	private static float ResolveImageStartX(ParagraphBlock paragraphBlock, LayoutBlockPlacement placement, List<ImageLayoutItem> items)
	{
		if (items.Any(i => i.IsTab))
		{
			return placement.XTwips;
		}

		var totalWidth = items.Sum(i => i.Width);
		return paragraphBlock.Alignment switch
		{
			ParagraphAlignment.Center => placement.XTwips + ((placement.ContentWidthTwips - totalWidth) / 2f),
			ParagraphAlignment.Right => placement.XTwips + placement.ContentWidthTwips - totalWidth,
			_ => placement.XTwips,
		};
	}

	private static float ResolveImageTabX(List<ImageLayoutItem> items, int itemIndex, TabStopProfile tabProfile, LayoutBlockPlacement placement, float currentX)
	{
		var contentWidthAfterTab = items.Skip(itemIndex + 1).Where(i => !i.IsTab).Sum(i => i.Width);
		if (items[itemIndex].IsPtabRightMargin)
		{
			return placement.XTwips + placement.ContentWidthTwips - contentWidthAfterTab;
		}

		var tabStop = tabProfile.ResolveNextTabStop(currentX - placement.XTwips);
		return tabStop.Type is TabStopType.Right or TabStopType.Center or TabStopType.Decimal
			? placement.XTwips + TabStopResolver.ComputeContentStart(tabStop, contentWidthAfterTab)
			: placement.XTwips + tabStop.PositionTwips;
	}

	private static void EmitDrawingImage(
		Drawing drawing,
		IRenderTarget target,
		SectionInfo? section,
		IReadOnlyDictionary<string, ImageData> images,
		LayoutBlockPlacement placement,
		float currentX,
		float currentY)
	{
		var inline = drawing.GetFirstChild<DW.Inline>();
		if (inline is not null)
		{
			EmitInlineImage(inline, target, images, currentX, currentY);
			return;
		}

		var anchor = drawing.GetFirstChild<DW.Anchor>();
		if (anchor is not null && section is not null)
		{
			EmitAnchorImage(anchor, target, section, images, placement);
		}
	}

	private static void EmitInlineImage(DW.Inline inline, IRenderTarget target, IReadOnlyDictionary<string, ImageData> images, float currentX, float currentY)
	{
		var relId = inline.Descendants<A.Blip>().FirstOrDefault()?.Embed?.Value;
		if (string.IsNullOrEmpty(relId) || !images.TryGetValue(relId, out var imageData))
		{
			return;
		}

		var extent = inline.Extent;
		var widthTwips = TwipConverter.EmusToTwips(extent?.Cx ?? 0);
		var heightTwips = TwipConverter.EmusToTwips(extent?.Cy ?? 0);
		if (widthTwips > 0f && heightTwips > 0f)
		{
			target.DrawImage(imageData, new RenderRect(currentX, currentY, widthTwips, heightTwips));
		}
	}

	private static void EmitAnchorImage(DW.Anchor anchor, IRenderTarget target, SectionInfo section, IReadOnlyDictionary<string, ImageData> images, LayoutBlockPlacement placement)
	{
		var anchorRelId = anchor.Descendants<A.Blip>().FirstOrDefault()?.Embed?.Value;
		if (string.IsNullOrEmpty(anchorRelId) || !images.TryGetValue(anchorRelId, out var anchorImageData))
		{
			return;
		}

		var anchorExtent = anchor.Extent;
		var anchorWidthEmu = anchorExtent?.Cx ?? 0;
		var anchorHeightEmu = anchorExtent?.Cy ?? 0;
		var anchorWidthTwips = TwipConverter.EmusToTwips(anchorWidthEmu);
		var anchorHeightTwips = TwipConverter.EmusToTwips(anchorHeightEmu);
		if (anchorWidthTwips <= 0f || anchorHeightTwips <= 0f)
		{
			return;
		}

		var anchorPlacement = RunElementParser.ParseAnchorPlacement(anchor);
		var resolved = AnchorPositionResolver.ResolveAbsolutePosition(
			anchorPlacement,
			anchorWidthEmu,
			anchorHeightEmu,
			section,
			placement.XTwips,
			placement.YTwips,
			placement.ContentWidthTwips);

		target.DrawImage(anchorImageData, new RenderRect(resolved.X, resolved.Y, anchorWidthTwips, anchorHeightTwips));
	}
	private static void EmitLeaderCharacters(TabStopLeader leader, float fromX, float toX, float baselineY, RenderFont font, RenderBrush brush, IRenderTarget target)
	{
		if (leader == TabStopLeader.None || toX <= fromX)
		{
			return;
		}

		var leaderChar = leader switch
		{
			TabStopLeader.Dot => ".",
			TabStopLeader.Hyphen => "-",
			TabStopLeader.Underscore => "_",
			TabStopLeader.Heavy => "_",
			TabStopLeader.MiddleDot => "\u00B7",
			_ => null
		};

		if (leaderChar is null)
		{
			return;
		}

		var charWidth = EstimateTextWidthTwips(leaderChar, font.SizePoints);
		if (charWidth <= 0f)
		{
			return;
		}

		// Add small padding at start and end to avoid touching adjacent text
		var leaderFont = leader == TabStopLeader.Heavy ? font with { IsBold = true } : font;
		var x = fromX + charWidth;
		while (x + charWidth <= toX)
		{
			target.DrawText(leaderChar, x, baselineY, leaderFont, brush);
			x += charWidth;
		}
	}

	private static void EmitHeaderFooterBlocks(IReadOnlyList<LayoutBlock>? blocks, float topYTwips, LayoutPage page, BlockEmitContext context)
	{
		if (blocks is null or { Count: 0 })
		{
			return;
		}

		var currentY = topYTwips;
		foreach (var layoutBlock in blocks)
		{
			var maxInlineImageHeight = 0f;
			if (layoutBlock.Block is ParagraphBlock paragraphBlock)
			{
				maxInlineImageHeight = GetMaxInlineImageHeightTwips(paragraphBlock.SourceElement);
				EmitHeaderFooterParagraph(layoutBlock, paragraphBlock, currentY, maxInlineImageHeight, page, context);
			}

			currentY += MathF.Max(layoutBlock.HeightTwips, maxInlineImageHeight);
		}
	}

	private static void EmitHeaderFooterParagraph(LayoutBlock layoutBlock, ParagraphBlock paragraphBlock, float currentY, float maxInlineImageHeight, LayoutPage page, BlockEmitContext context)
	{
		var defaultFont = context.DefaultFont;
		var contentWidth = page.Section.PageWidth - page.Section.MarginLeft - page.Section.MarginRight;
		var segments = BuildTextSegments(paragraphBlock.SourceElement, defaultFont, context.FontFamily, page.PageNumber, context.TotalPageCount, context.RenderTimestampUtc);
		var hfFontSize = segments.Count > 0 ? segments[0].Font.SizePoints : defaultFont.SizePoints;
		var lineHeightTwips = MathF.Max(layoutBlock.HeightTwips, maxInlineImageHeight);
		var fontHeightTwips = TwipConverter.PointsToTwips(hfFontSize);
		var fontDescentTwips = fontHeightTwips * 0.2f;
		var baselineOffset = MathF.Max(fontHeightTwips, lineHeightTwips - fontDescentTwips);
		var baselineY = currentY + baselineOffset;
		var hfPlacement = new LayoutBlockPlacement(layoutBlock, (float)page.Section.MarginLeft, currentY, contentWidth, 0);

		EmitInlineSegments(segments, paragraphBlock, hfPlacement, baselineY, (float)page.Section.MarginLeft, context.Target, includeHyperlinks: false);

		if (paragraphBlock.Borders.HasAnyVisibleBorder)
		{
			EmitParagraphBorders(paragraphBlock.Borders, hfPlacement.XTwips, hfPlacement.YTwips, lineHeightTwips, hfPlacement.ContentWidthTwips, context.Target);
		}

		EmitParagraphImagesIfAny(paragraphBlock, hfPlacement, context);
	}
	private static float GetMaxInlineImageHeightTwips(Paragraph paragraph)
	{
		ArgumentNullException.ThrowIfNull(paragraph);

		var maxHeight = 0f;
		foreach (var drawing in paragraph.Descendants<Drawing>())
		{
			var inline = drawing.GetFirstChild<DW.Inline>();
			if (inline is null)
			{
				continue;
			}

			var heightTwips = TwipConverter.EmusToTwips(inline.Extent?.Cy ?? 0);
			if (heightTwips > maxHeight)
			{
				maxHeight = heightTwips;
			}
		}

		return maxHeight;
	}

	private static void EmitWatermark(LayoutPage page, WatermarkInfo watermark, IRenderTarget target)
	{
		var centerX = watermark.IsHorizontallyCentered
			? page.Section.PageWidth / 2f
			: page.Section.MarginLeft;
		var centerY = watermark.IsVerticallyCentered
			? page.Section.PageHeight / 2f
			: page.Section.MarginTop;

		if (watermark.Kind == WatermarkKind.Text && !string.IsNullOrWhiteSpace(watermark.Text))
		{
			EmitTextWatermark(watermark, centerX, centerY, target);
		}
		else if (watermark.Kind == WatermarkKind.Image && watermark.ResolvedImageData is not null)
		{
			target.DrawRotatedImage(
				watermark.ResolvedImageData,
				centerX,
				centerY,
				watermark.WidthTwips,
				watermark.HeightTwips,
				watermark.RotationDegrees,
				watermark.Opacity);
		}
	}

	private static void EmitTextWatermark(WatermarkInfo watermark, float centerX, float centerY, IRenderTarget target)
	{
		var opacity = (byte)Math.Clamp(watermark.Opacity * 255f, 0, 255);
		var color = ResolveWatermarkColor(watermark.FillColor, opacity);
		var brush = new SolidRenderBrush(color);
		var fontFamily = string.IsNullOrWhiteSpace(watermark.FontFamily) ? "Calibri" : watermark.FontFamily;
		var fontSize = EstimateWatermarkFontSize(watermark);
		var font = new RenderFont(fontFamily, fontSize);

		target.DrawRotatedText(watermark.Text!, centerX, centerY, watermark.RotationDegrees, font, brush);
	}

	private static RenderColor ResolveWatermarkColor(string? fillColor, byte opacity)
	{
		if (string.IsNullOrWhiteSpace(fillColor))
		{
			return new RenderColor(192, 192, 192, opacity);
		}

		if (fillColor.StartsWith('#') && fillColor.Length == 7)
		{
			var r = byte.Parse(fillColor.AsSpan(1, 2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture);
			var g = byte.Parse(fillColor.AsSpan(3, 2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture);
			var b = byte.Parse(fillColor.AsSpan(5, 2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture);
			return new RenderColor(r, g, b, opacity);
		}

		return fillColor.ToUpperInvariant() switch
		{
			"SILVER" => new RenderColor(192, 192, 192, opacity),
			"GRAY" or "GREY" => new RenderColor(128, 128, 128, opacity),
			"RED" => new RenderColor(255, 0, 0, opacity),
			"BLUE" => new RenderColor(0, 0, 255, opacity),
			"GREEN" => new RenderColor(0, 128, 0, opacity),
			"BLACK" => new RenderColor(0, 0, 0, opacity),
			"WHITE" => new RenderColor(255, 255, 255, opacity),
			_ => new RenderColor(192, 192, 192, opacity)
		};
	}

	private static float EstimateWatermarkFontSize(WatermarkInfo watermark)
	{
		if (watermark.WidthTwips <= 0 || string.IsNullOrEmpty(watermark.Text))
		{
			return 72f;
		}

		// Estimate: width covers the text at approximately 0.6 * fontSize per character
		var charCount = watermark.Text.Length;
		var estimatedSizePoints = TwipConverter.TwipsToPoints(watermark.WidthTwips) / (charCount * 0.6f);
		return Math.Clamp(estimatedSizePoints, 8f, 200f);
	}
}

