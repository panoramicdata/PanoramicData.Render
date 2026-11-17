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
}
