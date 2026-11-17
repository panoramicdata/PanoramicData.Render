namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;

internal static partial class DocumentLayoutEngine
{
	/// <summary>
	/// Returns the natural (single-spaced) line height in twips derived from the font size of
	/// the first text run in <paramref name="para"/>.
	/// The <c>w:sz</c> attribute stores size in half-points; converting to twips:
	/// half-points ÷ 2 = points; points × 20 = twips → half-points × 10 = twips.
	/// Falls back to <paramref name="fallbackLineHeight"/> when no font size can be determined.
	/// </summary>
	private static float GetNaturalLineHeightFromParagraph(ParagraphBlock para, float fallbackLineHeight)
	{
		foreach (var run in para.SourceElement.Descendants<Run>())
		{
			var szValue = run.RunProperties?.FontSize?.Val?.Value;
			if (szValue is not null
				&& float.TryParse(szValue, System.Globalization.CultureInfo.InvariantCulture, out var halfPoints)
				&& halfPoints > 0f)
			{
				return halfPoints * 10f;
			}
		}

		return fallbackLineHeight;
	}

	/// <summary>
	/// Scans a paragraph for inline images and returns the maximum image height in twips.
	/// Returns 0 when no inline images are present.
	/// </summary>
	private static float ComputeMaxInlineImageHeight(ParagraphBlock para)
	{
		var maxHeight = 0f;
		foreach (var drawing in para.SourceElement.Descendants<Drawing>())
		{
			var inline = drawing.GetFirstChild<DW.Inline>();
			if (inline?.Extent is { Cy: { } cy })
			{
				var heightTwips = TwipConverter.EmusToTwips(cy);
				if (heightTwips > maxHeight)
				{
					maxHeight = heightTwips;
				}
			}
		}

		return maxHeight;
	}

	/// <summary>
	/// Resolves the paragraph spacing from the materialized paragraph properties.
	/// </summary>
	private static ParagraphSpacing ResolveParagraphSpacing(ParagraphBlock para, ParagraphSpacing baseline = default)
	{
		var spacingElement = para.SourceElement.ParagraphProperties?.SpacingBetweenLines;
		if (spacingElement is null)
		{
			return baseline;
		}

		// Merge direct formatting on top of the baseline: only override what is explicitly set.
		var before = ParseTwipsOr(spacingElement.Before?.Value, baseline.SpaceBefore);
		var after = ParseTwipsOr(spacingElement.After?.Value, baseline.SpaceAfter);
		var line = ParseTwipsOr(spacingElement.Line?.Value, baseline.LineSpacingTwips);
		var lineRule = MapLineRule(spacingElement.LineRule?.Value, baseline.LineRule);

		return new ParagraphSpacing(before, after, line, lineRule);
	}

	private static float ParseTwipsOr(string? value, float fallback)
		=> value is not null ? ParseTwips(value) : fallback;

	// Auto is the default, so any other explicit rule maps to null; an unset rule inherits.
}
