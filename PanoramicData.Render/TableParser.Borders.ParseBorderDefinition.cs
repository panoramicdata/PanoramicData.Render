namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class TableParser
{
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
