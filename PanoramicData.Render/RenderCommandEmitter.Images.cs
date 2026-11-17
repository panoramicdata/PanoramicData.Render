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
}
