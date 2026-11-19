namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;

internal static partial class RunElementParser
{
	/// <summary>
	/// Parses the drawings in an <c>mc:Choice</c> branch, using <c>mc:Fallback</c> only when the choice yields none.
	/// </summary>
	private static void ParseAlternateContent(AlternateContent alternateContent, List<RunElement> elements)
	{
		var beforeCount = elements.Count;

		foreach (var drawing in alternateContent.GetFirstChild<AlternateContentChoice>()?.Descendants<Drawing>() ?? [])
		{
			ParseDrawing(drawing, elements);
		}

		if (elements.Count > beforeCount)
		{
			return;
		}

		foreach (var drawing in alternateContent.GetFirstChild<AlternateContentFallback>()?.Descendants<Drawing>() ?? [])
		{
			ParseDrawing(drawing, elements);
		}
	}

	private static void ParseDrawing(Drawing drawing, List<RunElement> elements)
	{
		var inline = drawing.GetFirstChild<DW.Inline>();
		if (inline is not null)
		{
			elements.Add(ParseInlineDrawing(inline));
			return;
		}

		var anchor = drawing.GetFirstChild<DW.Anchor>();
		if (anchor is not null)
		{
			elements.Add(ParseAnchorDrawing(anchor));
		}
	}

	private static RunElement ParseInlineDrawing(DW.Inline inline)
	{
		var (widthEmu, heightEmu) = GetExtentEmu(inline.Extent);
		var graphic = ParseGraphicContent(inline, widthEmu, heightEmu, null);
		if (graphic is not null)
		{
			return graphic;
		}

		var crop = ParseCrop(inline.Descendants<A.SourceRectangle>().FirstOrDefault());
		return new InlineImageRunElement
		{
			RelationshipId = GetBlipRelationshipId(inline),
			WidthEmu = widthEmu,
			HeightEmu = heightEmu,
			CropLeft = crop.Left,
			CropTop = crop.Top,
			CropRight = crop.Right,
			CropBottom = crop.Bottom
		};
	}

	private static RunElement ParseAnchorDrawing(DW.Anchor anchor)
	{
		var (widthEmu, heightEmu) = GetExtentEmu(anchor.Extent);
		var placement = ParseAnchorPlacement(anchor);
		var graphic = ParseGraphicContent(anchor, widthEmu, heightEmu, placement);
		if (graphic is not null)
		{
			return graphic;
		}

		var crop = ParseCrop(anchor.Descendants<A.SourceRectangle>().FirstOrDefault());
		return new AnchorImageRunElement
		{
			RelationshipId = GetBlipRelationshipId(anchor),
			WidthEmu = widthEmu,
			HeightEmu = heightEmu,
			CropLeft = crop.Left,
			CropTop = crop.Top,
			CropRight = crop.Right,
			CropBottom = crop.Bottom,
			Placement = placement
		};
	}

	private static (long Cx, long Cy) GetExtentEmu(DW.Extent? extent)
		=> (extent?.Cx ?? 0, extent?.Cy ?? 0);

	private static string GetBlipRelationshipId(OpenXmlElement container)
		=> container.Descendants<A.Blip>().FirstOrDefault()?.Embed?.Value ?? string.Empty;

	private static (int Left, int Top, int Right, int Bottom) ParseCrop(A.SourceRectangle? sourceRectangle)
		=> (ParsePercentage(sourceRectangle?.Left), ParsePercentage(sourceRectangle?.Top), ParsePercentage(sourceRectangle?.Right), ParsePercentage(sourceRectangle?.Bottom));
}
