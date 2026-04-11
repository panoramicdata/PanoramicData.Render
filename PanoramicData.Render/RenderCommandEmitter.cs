namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text;

/// <summary>
/// Emits rendering commands from paginated layout blocks.
/// </summary>
internal static class RenderCommandEmitter
{
	private const float DefaultTextBaselineOffsetTwips = 240f;
	private const float AverageGlyphWidthFactor = 10f;
	private const float DefaultListIndentStepTwips = 360f;
	private const float DefaultListTextGapTwips = 240f;
	private static readonly RenderColor DefaultTextColor = new(0, 0, 0);

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
	public static void EmitPage(LayoutPage page, IRenderTarget target, RenderOptions? options = null, int? totalPageCount = null, DateTime? renderTimestampUtc = null, ListNumberingState? listState = null)
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

		if (page.Watermark is { } watermark)
		{
			EmitWatermark(page, watermark, target);
		}

		foreach (var placement in GetBlockPlacements(page))
		{
			var layoutBlock = placement.Block;
			var yTwips = placement.YTwips;
			switch (layoutBlock.Block)
			{
				case ParagraphBlock paragraphBlock:
				{
						foreach (var bookmark in paragraphBlock.BookmarkStarts)
						{
							target.SetNamedDestination(bookmark.Name, placement.XTwips, yTwips);
						}

						var baselineOffset = MathF.Min(DefaultTextBaselineOffsetTwips, layoutBlock.HeightTwips);
						var baselineY = yTwips + baselineOffset;
						var segments = BuildTextSegments(paragraphBlock.SourceElement, defaultFont, fontFamily, page.PageNumber, effectiveTotalPageCount, effectiveTimestampUtc);
						var currentX = placement.XTwips;

						if (paragraphBlock.NumberingId is int numberingId && paragraphBlock.NumberingLevel is int numberingLevel)
						{
							var listStyle = ResolveListStyle(renderOptions, numberingId, numberingLevel);
							var labelResult = effectiveListState.Advance(numberingId, listStyle);
							var labelText = string.IsNullOrEmpty(labelResult.Label) ? string.Empty : labelResult.Label + " ";
							if (!string.IsNullOrEmpty(labelText))
							{
								var labelFontFamily = string.IsNullOrWhiteSpace(listStyle.FontFamily) ? defaultFont.Family : listStyle.FontFamily;
								var labelFont = defaultFont with { Family = labelFontFamily };
								var labelWidth = EstimateTextWidthTwips(labelText, labelFont.SizePoints);
								var textStartX = placement.XTwips + ((numberingLevel + 1) * DefaultListIndentStepTwips) + DefaultListTextGapTwips;
								var labelX = textStartX - labelWidth;
								target.DrawText(labelText, labelX, baselineY, labelFont, defaultBrush);
								currentX = textStartX;
							}
						}

						foreach (var segment in segments)
					{
							target.DrawText(segment.Text, currentX, baselineY, segment.Font, defaultBrush);
							var segmentWidth = EstimateTextWidthTwips(segment.Text, segment.Font.SizePoints);
							if (!string.IsNullOrWhiteSpace(segment.HyperlinkUri))
							{
								var textHeight = EstimateTextHeightTwips(segment.Font.SizePoints);
								target.SetHyperlink(new RenderRect(currentX, baselineY - textHeight, segmentWidth, textHeight), segment.HyperlinkUri);
							}

							currentX += segmentWidth;
					}
					break;
				}
				case TablePlaceholderBlock:
				{
					var heightTwips = MathF.Max(layoutBlock.HeightTwips, 1f);
					target.DrawRect(new RenderRect(placement.XTwips, yTwips, placement.ContentWidthTwips, heightTwips), null, defaultStroke);
					break;
				}
			}
		}
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

	private static NumberingLevelStyle ResolveListStyle(RenderOptions options, int numberingId, int numberingLevel)
	{
		var styleKey = CreateNumberingStyleKey(numberingId, numberingLevel);
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
		var activeFields = new Stack<ActiveField>();
		foreach (var child in paragraph.ChildElements)
		{
			AppendSegmentsFromElement(child, segments, defaultFont, defaultFamily, activeFields, currentPageNumber, totalPageCount, renderTimestampUtc, part);
		}

		if (segments.Count == 0)
		{
			var text = paragraph.InnerText;
			if (!string.IsNullOrWhiteSpace(text))
			{
				segments.Add(new TextSegment(text, defaultFont, null));
			}
		}

		return segments;
	}

	private static void AppendSegmentsFromElement(OpenXmlElement element, List<TextSegment> segments, RenderFont defaultFont, string defaultFamily, Stack<ActiveField> activeFields, int currentPageNumber, int totalPageCount, DateTime renderTimestampUtc, OpenXmlPart? part = null, string? hyperlinkUri = null)
	{
		switch (element)
		{
			case Run run:
				AppendSegmentsFromRun(run, segments, defaultFont, defaultFamily, activeFields, currentPageNumber, totalPageCount, renderTimestampUtc, hyperlinkUri);
				break;
			case SimpleField simpleField:
				AppendSegmentsFromSimpleField(simpleField, segments, defaultFont, defaultFamily, currentPageNumber, totalPageCount, renderTimestampUtc);
				break;
			case Hyperlink hyperlink:
				var resolvedUri = RunElementParser.ResolveHyperlinkUri(hyperlink, part);
				foreach (var child in hyperlink.ChildElements)
				{
					AppendSegmentsFromElement(child, segments, defaultFont, defaultFamily, activeFields, currentPageNumber, totalPageCount, renderTimestampUtc, part, resolvedUri);
				}

				break;
			default:
				foreach (var child in element.ChildElements)
				{
					AppendSegmentsFromElement(child, segments, defaultFont, defaultFamily, activeFields, currentPageNumber, totalPageCount, renderTimestampUtc, part, hyperlinkUri);
				}
				break;
		}
	}

	private static void AppendSegmentsFromRun(Run run, List<TextSegment> segments, RenderFont defaultFont, string defaultFamily, Stack<ActiveField> activeFields, int currentPageNumber, int totalPageCount, DateTime renderTimestampUtc, string? hyperlinkUri = null)
	{
		var runProperties = run.RunProperties;
		var font = ResolveRunFont(runProperties, defaultFont, defaultFamily);

		foreach (var fieldCode in run.Elements<FieldCode>())
		{
			if (activeFields.Count > 0)
			{
				activeFields.Peek().InstructionBuilder.Append(fieldCode.Text);
			}
		}

		foreach (var fieldChar in run.Elements<FieldChar>())
		{
			HandleFieldChar(fieldChar, activeFields);
		}

		var text = string.Concat(run.Elements<Text>().Select(t => t.Text));
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		if (activeFields.Count == 0)
		{
			AppendTextSegment(segments, text, font, hyperlinkUri);
			return;
		}

		var activeField = activeFields.Peek();
		if (!activeField.IsResultSection)
		{
			return;
		}

		if (activeField.Kind is FieldKind.Page or FieldKind.NumPages or FieldKind.Date or FieldKind.Time)
		{
			if (!activeField.HasRenderedComputedValue)
			{
				AppendTextSegment(segments, ComputeFieldValue(activeField.Kind, currentPageNumber, totalPageCount, renderTimestampUtc), font, null);
				activeField.HasRenderedComputedValue = true;
			}

			return;
		}

		AppendTextSegment(segments, text, font, activeField.HyperlinkUri);
	}

	private static void AppendSegmentsFromSimpleField(SimpleField simpleField, List<TextSegment> segments, RenderFont defaultFont, string defaultFamily, int currentPageNumber, int totalPageCount, DateTime renderTimestampUtc)
	{
		var instructionText = simpleField.Instruction?.Value;
		var hyperlinkUri = ExtractHyperlinkUri(instructionText);
		var kind = ParseFieldKind(simpleField.Instruction?.Value);
		if (kind is FieldKind.Page or FieldKind.NumPages or FieldKind.Date or FieldKind.Time)
		{
			AppendTextSegment(segments, ComputeFieldValue(kind, currentPageNumber, totalPageCount, renderTimestampUtc), defaultFont, null);
			return;
		}

		var text = string.Concat(simpleField.Descendants<Text>().Select(t => t.Text));
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}

		var firstRunProperties = simpleField.Descendants<Run>().Select(r => r.RunProperties).FirstOrDefault(r => r is not null);
		var font = ResolveRunFont(firstRunProperties, defaultFont, defaultFamily);
		AppendTextSegment(segments, text, font, hyperlinkUri);
	}

	private static void HandleFieldChar(FieldChar fieldChar, Stack<ActiveField> activeFields)
	{
		if (fieldChar.FieldCharType is null)
		{
			return;
		}

		if (fieldChar.FieldCharType == FieldCharValues.Begin)
		{
			activeFields.Push(new ActiveField());
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
		return new RenderFont(
			string.IsNullOrWhiteSpace(fontFamily) ? defaultFamily : fontFamily,
			defaultFont.SizePoints,
			IsOn(runProperties?.Bold),
			IsOn(runProperties?.Italic),
			IsUnderline(runProperties?.Underline),
			IsOn(runProperties?.Strike));
	}

	private static void AppendTextSegment(List<TextSegment> segments, string text, RenderFont font, string? hyperlinkUri)
	{
		if (segments.Count > 0 && segments[^1].Font == font && string.Equals(segments[^1].HyperlinkUri, hyperlinkUri, StringComparison.Ordinal))
		{
			segments[^1] = segments[^1] with { Text = segments[^1].Text + text };
		}
		else
		{
			segments.Add(new TextSegment(text, font, hyperlinkUri));
		}
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

	private static string ComputeFieldValue(FieldKind fieldKind, int currentPageNumber, int totalPageCount, DateTime renderTimestampUtc)
	{
		return fieldKind switch
		{
			FieldKind.Page => currentPageNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
			FieldKind.NumPages => totalPageCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
			FieldKind.Date => renderTimestampUtc.ToString("d", System.Globalization.CultureInfo.InvariantCulture),
			FieldKind.Time => renderTimestampUtc.ToString("T", System.Globalization.CultureInfo.InvariantCulture),
			_ => string.Empty
		};
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

	private static float EstimateTextHeightTwips(float sizePoints)
	{
		return MathF.Max(TwipConverter.PointsToTwips(sizePoints) * 1.2f, 1f);
	}

	private sealed class ActiveField
	{
		public StringBuilder InstructionBuilder { get; } = new();

		public bool IsResultSection { get; set; }

		public FieldKind Kind { get; set; } = FieldKind.Other;

		public string? HyperlinkUri { get; set; }

		public bool HasRenderedComputedValue { get; set; }
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

	private readonly record struct TextSegment(string Text, RenderFont Font, string? HyperlinkUri);

	private static void EmitWatermark(LayoutPage page, WatermarkInfo watermark, IRenderTarget target)
	{
		var centerX = watermark.IsHorizontallyCentered
			? page.Section.PageWidth / 2f
			: page.Section.MarginLeft;
		var centerY = watermark.IsVerticallyCentered
			? page.Section.PageHeight / 2f
			: page.Section.MarginTop;

		switch (watermark.Kind)
		{
			case WatermarkKind.Text when !string.IsNullOrWhiteSpace(watermark.Text):
				EmitTextWatermark(watermark, centerX, centerY, target);
				break;

			case WatermarkKind.Image when watermark.ResolvedImageData is not null:
				target.DrawRotatedImage(
					watermark.ResolvedImageData,
					centerX,
					centerY,
					watermark.WidthTwips,
					watermark.HeightTwips,
					watermark.RotationDegrees,
					watermark.Opacity);
				break;
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
