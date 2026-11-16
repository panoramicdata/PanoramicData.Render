namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using A = DocumentFormat.OpenXml.Drawing;

/// <summary>
/// Parses DrawingML shape fill definitions.
/// </summary>
internal static class ShapeFillParser
{
	/// <summary>
	/// Parses shape fill information from shape properties.
	/// </summary>
	/// <param name="shapeProperties">The shape properties element.</param>
	/// <returns>The parsed shape fill information.</returns>
	public static ShapeFillInfo Parse(A.ShapeProperties? shapeProperties)
	{
		if (shapeProperties is null)
		{
			return ShapeFillInfo.None;
		}

		return ParseSolid(shapeProperties)
			?? ParseGradient(shapeProperties)
			?? ParsePattern(shapeProperties)
			?? ParsePicture(shapeProperties)
			?? ShapeFillInfo.None;
	}

	private static ShapeFillInfo? ParseSolid(A.ShapeProperties shapeProperties)
	{
		var solidFill = shapeProperties.GetFirstChild<A.SolidFill>();
		if (solidFill is null)
		{
			return null;
		}

		return new ShapeFillInfo
		{
			Kind = ShapeFillKind.Solid,
			SolidColorHex = ExtractHexColor(solidFill)
		};
	}

	private static ShapeFillInfo? ParseGradient(A.ShapeProperties shapeProperties)
	{
		var gradientFill = shapeProperties.GetFirstChild<A.GradientFill>();
		if (gradientFill is null)
		{
			return null;
		}

		var style = gradientFill.GetFirstChild<A.LinearGradientFill>() is not null ? "linear" : "radial";
		var stops = gradientFill
			.Descendants<A.GradientStop>()
			.Select(s => new GradientStopInfo(
				Position: s.Position?.Value ?? 0,
				ColorHex: ExtractHexColor(s) ?? string.Empty))
			.ToArray();

		return new ShapeFillInfo
		{
			Kind = ShapeFillKind.Gradient,
			GradientStyle = style,
			GradientStops = stops
		};
	}

	private static ShapeFillInfo? ParsePattern(A.ShapeProperties shapeProperties)
	{
		var patternFill = shapeProperties.GetFirstChild<A.PatternFill>();
		if (patternFill is null)
		{
			return null;
		}

		return new ShapeFillInfo
		{
			Kind = ShapeFillKind.Pattern,
			PatternPreset = patternFill.Preset?.Value.ToString(),
			PatternForegroundColorHex = ExtractHexColor(patternFill.GetFirstChild<A.ForegroundColor>()),
			PatternBackgroundColorHex = ExtractHexColor(patternFill.GetFirstChild<A.BackgroundColor>())
		};
	}

	private static ShapeFillInfo? ParsePicture(A.ShapeProperties shapeProperties)
	{
		var blipFill = shapeProperties.GetFirstChild<A.BlipFill>();
		if (blipFill is null)
		{
			return null;
		}

		var blip = blipFill.GetFirstChild<A.Blip>();
		return new ShapeFillInfo
		{
			Kind = ShapeFillKind.Picture,
			PictureRelationshipId = blip?.Embed?.Value
		};
	}
	private static string? ExtractHexColor(OpenXmlElement? element)
	{
		if (element is null)
		{
			return null;
		}

		var rgb = element.Descendants<A.RgbColorModelHex>().FirstOrDefault();
		return rgb?.Val?.Value;
	}
}
