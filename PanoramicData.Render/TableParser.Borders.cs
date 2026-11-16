namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class TableParser
{
	internal static TableBorderSet ParseTableBorders(TableBorders? borders)
	{
		if (borders is null)
		{
			return TableBorderSet.None;
		}

		return new TableBorderSet(
			Top: ParseBorderDefinition(borders.GetFirstChild<TopBorder>()),
			Bottom: ParseBorderDefinition(borders.GetFirstChild<BottomBorder>()),
			Left: ParseBorderDefinition(borders.GetFirstChild<LeftBorder>()),
			Right: ParseBorderDefinition(borders.GetFirstChild<RightBorder>()),
			InsideHorizontal: ParseBorderDefinition(borders.GetFirstChild<InsideHorizontalBorder>()),
			InsideVertical: ParseBorderDefinition(borders.GetFirstChild<InsideVerticalBorder>()));
	}

	internal static TableBorderSet ParseCellBorders(TableCellBorders? borders)
	{
		if (borders is null)
		{
			return TableBorderSet.None;
		}

		return new TableBorderSet(
			Top: ParseBorderDefinition(borders.GetFirstChild<TopBorder>()),
			Bottom: ParseBorderDefinition(borders.GetFirstChild<BottomBorder>()),
			Left: ParseBorderDefinition(borders.GetFirstChild<LeftBorder>()),
			Right: ParseBorderDefinition(borders.GetFirstChild<RightBorder>()));
	}

	internal static ParagraphShading ParseShading(Shading? shading)
	{
		if (shading is null)
		{
			return ParagraphShading.None;
		}

		var pattern = ParseShadingPattern(shading.GetAttribute("val", WordprocessingNamespace).Value);
		var patternColor = NormalizeShadingColor(shading.Color?.Value);
		var fillColor = NormalizeShadingColor(shading.Fill?.Value);

		if (pattern == ShadingPattern.Clear && patternColor is null && fillColor is null)
		{
			return ParagraphShading.None;
		}

		return new ParagraphShading(pattern, patternColor, fillColor);
	}

	private static string? NormalizeShadingColor(string? value)
	{
		if (string.IsNullOrWhiteSpace(value)
			|| value.Equals("auto", StringComparison.OrdinalIgnoreCase)
			|| value.Equals("nil", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}

		return value.ToUpperInvariant();
	}

	private static ShadingPattern ParseShadingPattern(string? value)
	{
		return value?.ToLowerInvariant() switch
		{
			"solid" => ShadingPattern.Solid,
			"horzstripe" => ShadingPattern.HorizontalStripe,
			"vertstripe" => ShadingPattern.VerticalStripe,
			"reversediagstripe" => ShadingPattern.ReverseDiagonalStripe,
			"diagstripe" => ShadingPattern.DiagonalStripe,
			"horzcross" => ShadingPattern.HorizontalCross,
			"diagcross" => ShadingPattern.DiagonalCross,
			"thinhorzstripe" => ShadingPattern.ThinHorizontalStripe,
			"thinvertstripe" => ShadingPattern.ThinVerticalStripe,
			"thinreversediagstripe" => ShadingPattern.ThinReverseDiagonalStripe,
			"thindiagstripe" => ShadingPattern.ThinDiagonalStripe,
			"thinhorzcross" => ShadingPattern.ThinHorizontalCross,
			"thindiagcross" => ShadingPattern.ThinDiagonalCross,
			"pct5" => ShadingPattern.Percent5,
			"pct10" => ShadingPattern.Percent10,
			"pct12" => ShadingPattern.Percent12,
			"pct15" => ShadingPattern.Percent15,
			"pct20" => ShadingPattern.Percent20,
			"pct25" => ShadingPattern.Percent25,
			"pct30" => ShadingPattern.Percent30,
			"pct35" => ShadingPattern.Percent35,
			"pct37" => ShadingPattern.Percent37,
			"pct40" => ShadingPattern.Percent40,
			"pct45" => ShadingPattern.Percent45,
			"pct50" => ShadingPattern.Percent50,
			"pct55" => ShadingPattern.Percent55,
			"pct60" => ShadingPattern.Percent60,
			"pct62" => ShadingPattern.Percent62,
			"pct65" => ShadingPattern.Percent65,
			"pct70" => ShadingPattern.Percent70,
			"pct75" => ShadingPattern.Percent75,
			"pct80" => ShadingPattern.Percent80,
			"pct85" => ShadingPattern.Percent85,
			"pct87" => ShadingPattern.Percent87,
			"pct90" => ShadingPattern.Percent90,
			"pct95" => ShadingPattern.Percent95,
			_ => ShadingPattern.Clear,
		};
	}

	internal static TableBorderDefinition? ParseBorderDefinition(OpenXmlElement? borderElement)
	{
		BorderType? border = borderElement switch
		{
			TopBorder b => b,
			BottomBorder b => b,
			LeftBorder b => b,
			RightBorder b => b,
			InsideHorizontalBorder b => b,
			InsideVerticalBorder b => b,
			_ => null,
		};

		return border is null ? null : ParseBorderDefinition(border.Val?.Value, border.Size?.Value, border.Color?.Value);
	}

	private static TableBorderDefinition ParseBorderDefinition(BorderValues? value, UInt32Value? size, StringValue? color)
	{
		var width = size?.Value is { } sz ? (int)sz : 0;
		return new TableBorderDefinition(ParseBorderStyle(value), width, color?.Value);
	}

	private static readonly (BorderValues Value, BorderStyle Style)[] BorderStyleMap =
	[
		(BorderValues.Single, BorderStyle.Single),
		(BorderValues.Double, BorderStyle.Double),
		(BorderValues.Dotted, BorderStyle.Dotted),
		(BorderValues.Dashed, BorderStyle.Dashed),
		(BorderValues.DotDash, BorderStyle.DotDash),
		(BorderValues.DotDotDash, BorderStyle.DotDotDash),
		(BorderValues.Triple, BorderStyle.Triple),
		(BorderValues.Thick, BorderStyle.Thick),
		(BorderValues.ThinThickSmallGap, BorderStyle.ThinThickSmallGap),
		(BorderValues.ThickThinSmallGap, BorderStyle.ThickThinSmallGap),
		(BorderValues.ThinThickThinSmallGap, BorderStyle.ThinThickThinSmallGap),
		(BorderValues.Wave, BorderStyle.Wave),
		(BorderValues.DoubleWave, BorderStyle.DoubleWave),
		(BorderValues.ThreeDEmboss, BorderStyle.ThreeDEmboss),
		(BorderValues.ThreeDEngrave, BorderStyle.ThreeDEngrave),
	];

	internal static BorderStyle ParseBorderStyle(BorderValues? value)
	{
		foreach (var (candidate, style) in BorderStyleMap)
		{
			if (value == candidate)
			{
				return style;
			}
		}

		return BorderStyle.None;
	}
}