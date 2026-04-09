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

		var solidFill = shapeProperties.GetFirstChild<A.SolidFill>();
		if (solidFill is not null)
		{
			return new ShapeFillInfo
			{
				Kind = ShapeFillKind.Solid,
				SolidColorHex = ExtractHexColor(solidFill)
			};
		}

		var gradientFill = shapeProperties.GetFirstChild<A.GradientFill>();
		if (gradientFill is not null)
		{
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

		var patternFill = shapeProperties.GetFirstChild<A.PatternFill>();
		if (patternFill is not null)
		{
			return new ShapeFillInfo
			{
				Kind = ShapeFillKind.Pattern,
				PatternPreset = patternFill.Preset?.Value.ToString(),
				PatternForegroundColorHex = ExtractHexColor(patternFill.GetFirstChild<A.ForegroundColor>()),
				PatternBackgroundColorHex = ExtractHexColor(patternFill.GetFirstChild<A.BackgroundColor>())
			};
		}

		var blipFill = shapeProperties.GetFirstChild<A.BlipFill>();
		if (blipFill is not null)
		{
			var blip = blipFill.GetFirstChild<A.Blip>();
			return new ShapeFillInfo
			{
				Kind = ShapeFillKind.Picture,
				PictureRelationshipId = blip?.Embed?.Value
			};
		}

		return ShapeFillInfo.None;
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
