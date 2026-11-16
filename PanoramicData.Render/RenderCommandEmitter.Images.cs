namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using SkiaSharp;
using System.Globalization;
using System.Text;

internal static partial class RenderCommandEmitter
{
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
				if (CreateImageLayoutItem(child, defaultFont) is { } item)
				{
					items.Add(item);
				}
			}
		}

		return items;
	}

	private static ImageLayoutItem? CreateImageLayoutItem(OpenXmlElement child, RenderFont defaultFont)
		=> child switch
		{
			Text t => new ImageLayoutItem(false, false, false, EstimateTextWidthTwips(t.Text, defaultFont.SizePoints), null),
			TabChar => new ImageLayoutItem(true, false, false, 0f, null),
			PositionalTab pTab => new ImageLayoutItem(true, false, pTab.Alignment?.Value == AbsolutePositionTabAlignmentValues.Right, 0f, null),
			Drawing drawing => new ImageLayoutItem(false, true, false, GetInlineDrawingWidthTwips(drawing), drawing),
			_ => null,
		};

	private static float GetInlineDrawingWidthTwips(Drawing drawing)
	{
		var inlineExt = drawing.GetFirstChild<DW.Inline>()?.Extent;
		return inlineExt is not null ? TwipConverter.EmusToTwips(inlineExt.Cx ?? 0) : 0f;
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
		if (!TryGetImage(inline, images, out var imageData))
		{
			return;
		}

		var (widthEmu, heightEmu) = GetExtentEmu(inline.Extent);
		var widthTwips = TwipConverter.EmusToTwips(widthEmu);
		var heightTwips = TwipConverter.EmusToTwips(heightEmu);
		if (widthTwips > 0f && heightTwips > 0f)
		{
			target.DrawImage(imageData, new RenderRect(currentX, currentY, widthTwips, heightTwips));
		}
	}

	private static void EmitAnchorImage(DW.Anchor anchor, IRenderTarget target, SectionInfo section, IReadOnlyDictionary<string, ImageData> images, LayoutBlockPlacement placement)
	{
		if (!TryGetImage(anchor, images, out var anchorImageData))
		{
			return;
		}

		var (anchorWidthEmu, anchorHeightEmu) = GetExtentEmu(anchor.Extent);
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

	private static bool TryGetImage(OpenXmlElement drawingContainer, IReadOnlyDictionary<string, ImageData> images, out ImageData imageData)
	{
		var relId = drawingContainer.Descendants<A.Blip>().FirstOrDefault()?.Embed?.Value;
		if (string.IsNullOrEmpty(relId))
		{
			imageData = default!;
			return false;
		}

		return images.TryGetValue(relId, out imageData!);
	}

	private static (long Cx, long Cy) GetExtentEmu(DW.Extent? extent)
		=> (extent?.Cx ?? 0, extent?.Cy ?? 0);
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
