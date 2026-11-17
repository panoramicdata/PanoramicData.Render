namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;
using OoxmlSectionProperties = DocumentFormat.OpenXml.Wordprocessing.SectionProperties;
using OoxmlPBdr = DocumentFormat.OpenXml.Wordprocessing.ParagraphBorders;
using OoxmlBorderType = DocumentFormat.OpenXml.Wordprocessing.BorderType;

internal static partial class DocumentBlockParser
{
	private static ParagraphBorder? ParseBorderEdge(OoxmlBorderType? border)
	{
		if (border is null)
		{
			return null;
		}

		var style = MapBorderStyle(border.Val?.Value);
		if (style == BorderStyle.None)
		{
			return null;
		}

		return new ParagraphBorder(style, ParseBorderSize(border), ParseBorderSpace(border), border.Color?.Value);
	}

	private static int ParseBorderSize(OoxmlBorderType border) => border.Size?.Value is { } size ? (int)size : 0;

	private static float ParseBorderSpace(OoxmlBorderType border) => border.Space?.Value is { } space ? (float)space : 0f;

	private static readonly (BorderValues Value, BorderStyle Style)[] BorderStyleMap =
	[
		(BorderValues.Single, BorderStyle.Single),
		(BorderValues.Thick, BorderStyle.Thick),
		(BorderValues.Double, BorderStyle.Double),
		(BorderValues.Dotted, BorderStyle.Dotted),
		(BorderValues.Dashed, BorderStyle.Dashed),
		(BorderValues.DashSmallGap, BorderStyle.Dashed),
		(BorderValues.DashDotStroked, BorderStyle.Dashed),
		(BorderValues.DotDash, BorderStyle.DotDash),
		(BorderValues.DotDotDash, BorderStyle.DotDotDash),
		(BorderValues.Triple, BorderStyle.Triple),
		(BorderValues.ThinThickSmallGap, BorderStyle.ThinThickSmallGap),
		(BorderValues.ThickThinSmallGap, BorderStyle.ThickThinSmallGap),
		(BorderValues.ThinThickThinSmallGap, BorderStyle.ThinThickThinSmallGap),
		(BorderValues.Wave, BorderStyle.Wave),
		(BorderValues.DoubleWave, BorderStyle.DoubleWave),
	];

	private static BorderStyle MapBorderStyle(BorderValues? val)
	{
		if (val is null || val == BorderValues.None || val == BorderValues.Nil)
		{
			return BorderStyle.None;
		}

		foreach (var (value, style) in BorderStyleMap)
		{
			if (val == value)
			{
				return style;
			}
		}

		// Fall back to Single for any other visible border type.
		return BorderStyle.Single;
	}
	private static readonly (JustificationValues Value, ParagraphAlignment Alignment)[] JustificationMap =
	[
		(JustificationValues.Left, ParagraphAlignment.Left),
		(JustificationValues.Start, ParagraphAlignment.Left),
		(JustificationValues.Center, ParagraphAlignment.Center),
		(JustificationValues.Right, ParagraphAlignment.Right),
		(JustificationValues.End, ParagraphAlignment.Right),
		(JustificationValues.Both, ParagraphAlignment.Justified),
		(JustificationValues.Distribute, ParagraphAlignment.Justified),
	];
	/// <summary>
	/// Maps an OpenXML <see cref="Justification"/> element to a <see cref="ParagraphAlignment"/> value.
	/// </summary>
	private static ParagraphAlignment? MapJustification(Justification? jc)
	{
		if (jc?.Val?.Value is not { } value)
		{
			return null;
		}

		foreach (var (candidate, alignment) in JustificationMap)
		{
			if (value == candidate)
			{
				return alignment;
			}
		}

		return null;
	}
}
