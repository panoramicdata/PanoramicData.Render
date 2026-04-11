namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Vml;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Globalization;
using VmlImageData = DocumentFormat.OpenXml.Vml.ImageData;

/// <summary>
/// Parses watermark shapes from DOCX header parts.
/// </summary>
internal static class WatermarkParser
{
	private const string WatermarkIdMarker = "WaterMark";
	private const string TextPathShapeType = "#_x0000_t136";
	private const string PictureShapeType = "#_x0000_t75";

	/// <summary>
	/// Extracts watermarks from all header parts in the document.
	/// </summary>
	/// <param name="document">The Word processing document.</param>
	/// <returns>A list of parsed watermarks.</returns>
	public static IReadOnlyList<WatermarkInfo> ParseWatermarks(WordprocessingDocument document)
	{
		ArgumentNullException.ThrowIfNull(document);

		var mainPart = document.MainDocumentPart;
		if (mainPart is null)
		{
			return [];
		}

		var watermarks = new List<WatermarkInfo>();
		foreach (var headerPart in mainPart.HeaderParts)
		{
			var header = headerPart.Header;
			if (header is null)
			{
				continue;
			}

			foreach (var shape in header.Descendants<Shape>())
			{
				var watermark = TryParseWatermarkShape(shape);
				if (watermark is null)
				{
					continue;
				}

				if (watermark is { Kind: WatermarkKind.Image, ImageRelationshipId: not null })
				{
					var imageData = ResolveImageFromPart(headerPart, watermark.ImageRelationshipId);
					if (imageData is not null)
					{
						watermark = watermark with { ResolvedImageData = imageData };
					}
				}

				watermarks.Add(watermark);
			}
		}

		return watermarks;
	}

	/// <summary>
	/// Attempts to parse a VML <see cref="Shape"/> as a watermark.
	/// </summary>
	/// <param name="shape">The VML shape element.</param>
	/// <returns>A <see cref="WatermarkInfo"/> if the shape is a watermark; otherwise <see langword="null"/>.</returns>
	internal static WatermarkInfo? TryParseWatermarkShape(Shape shape)
	{
		ArgumentNullException.ThrowIfNull(shape);

		if (!IsWatermarkShape(shape))
		{
			return null;
		}

		var styleProps = VmlStyleParser.Parse(shape.Style?.Value);
		var widthTwips = styleProps.TryGetValue("width", out var w) ? VmlStyleParser.ParseDimensionToTwips(w) : 0f;
		var heightTwips = styleProps.TryGetValue("height", out var h) ? VmlStyleParser.ParseDimensionToTwips(h) : 0f;
		var rotation = styleProps.TryGetValue("rotation", out var r) ? VmlStyleParser.ParseRotation(r) : 0f;
		var isHCentered = styleProps.TryGetValue("mso-position-horizontal", out var hPos) &&
			string.Equals(hPos, "center", StringComparison.OrdinalIgnoreCase);
		var isVCentered = styleProps.TryGetValue("mso-position-vertical", out var vPos) &&
			string.Equals(vPos, "center", StringComparison.OrdinalIgnoreCase);

		var textPath = shape.GetFirstChild<TextPath>();
		if (textPath?.String?.Value is not null)
		{
			return ParseTextWatermark(shape, textPath, widthTwips, heightTwips, rotation, isHCentered, isVCentered);
		}

		var imageData = shape.GetFirstChild<VmlImageData>();
		if (imageData is not null)
		{
			return ParseImageWatermark(imageData, widthTwips, heightTwips, rotation, isHCentered, isVCentered);
		}

		return null;
	}

	private static bool IsWatermarkShape(Shape shape)
	{
		// Check if the shape ID contains "WaterMark" (case-insensitive per Word convention)
		if (shape.Id?.Value is string id &&
			id.Contains(WatermarkIdMarker, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		// Check known watermark shape types
		var shapeType = shape.Type?.Value;
		if (string.Equals(shapeType, TextPathShapeType, StringComparison.Ordinal) ||
			string.Equals(shapeType, PictureShapeType, StringComparison.Ordinal))
		{
			// Additional check: must be positioned absolutely (typical for watermarks)
			var styleProps = VmlStyleParser.Parse(shape.Style?.Value);
			return styleProps.TryGetValue("position", out var pos) &&
				string.Equals(pos, "absolute", StringComparison.OrdinalIgnoreCase);
		}

		return false;
	}

	private static WatermarkInfo ParseTextWatermark(
		Shape shape,
		TextPath textPath,
		float widthTwips,
		float heightTwips,
		float rotation,
		bool isHCentered,
		bool isVCentered)
	{
		var textStyleProps = VmlStyleParser.Parse(textPath.Style?.Value);
		var fontFamily = textStyleProps.TryGetValue("font-family", out var ff)
			? ff.Trim('"', '\'', '&')
			: null;
		var fillColor = shape.FillColor?.Value;
		var opacity = ParseOpacity(shape.GetFirstChild<Fill>()?.Opacity?.Value);

		return new WatermarkInfo
		{
			Kind = WatermarkKind.Text,
			Text = textPath.String?.Value,
			FontFamily = fontFamily,
			FillColor = fillColor,
			Opacity = opacity,
			RotationDegrees = rotation,
			WidthTwips = widthTwips,
			HeightTwips = heightTwips,
			IsHorizontallyCentered = isHCentered,
			IsVerticallyCentered = isVCentered
		};
	}

	private static WatermarkInfo ParseImageWatermark(
		VmlImageData imageData,
		float widthTwips,
		float heightTwips,
		float rotation,
		bool isHCentered,
		bool isVCentered)
	{
		var relId = imageData.RelationshipId?.Value ?? imageData.RelId?.Value;

		return new WatermarkInfo
		{
			Kind = WatermarkKind.Image,
			ImageRelationshipId = relId,
			RotationDegrees = rotation,
			WidthTwips = widthTwips,
			HeightTwips = heightTwips,
			IsHorizontallyCentered = isHCentered,
			IsVerticallyCentered = isVCentered
		};
	}

	private static float ParseOpacity(string? opacityValue)
	{
		if (string.IsNullOrWhiteSpace(opacityValue))
		{
			return 0.5f;
		}

		var trimmed = opacityValue.Trim();

		// Handle fractional values like ".5"
		if (float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue))
		{
			return Math.Clamp(floatValue, 0f, 1f);
		}

		// Handle Word's fixed-point format like "19661f" (value / 65536)
		if (trimmed.EndsWith('f') &&
			int.TryParse(trimmed[..^1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var fixedValue))
		{
			return Math.Clamp(fixedValue / 65536f, 0f, 1f);
		}

		return 0.5f;
	}

	private static ImageData? ResolveImageFromPart(OpenXmlPart part, string relationshipId)
	{
		if (!part.TryGetPartById(relationshipId, out var related) || related is not ImagePart imagePart)
		{
			return null;
		}

		using var stream = imagePart.GetStream(FileMode.Open, FileAccess.Read);
		using var ms = new MemoryStream();
		stream.CopyTo(ms);

		return new ImageData(ms.ToArray(), imagePart.ContentType);
	}
}
