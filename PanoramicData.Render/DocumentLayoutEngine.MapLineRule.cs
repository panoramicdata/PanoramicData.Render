namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;

internal static partial class DocumentLayoutEngine
{
	private static LineSpacingRule? MapLineRule(LineSpacingRuleValues? value, LineSpacingRule? inherited)
	{
		if (value is null)
		{
			return inherited;
		}

		if (value == LineSpacingRuleValues.Exact)
		{
			return LineSpacingRule.Exact;
		}

		return value == LineSpacingRuleValues.AtLeast ? LineSpacingRule.AtLeast : null;
	}

	/// <summary>
	/// Extracts the paragraph spacing from the document's <c>docDefaults</c> section.
	/// Returns <see cref="ParagraphSpacing.None"/> when styles is null or no default is set.
	/// </summary>
	internal static ParagraphSpacing ExtractDocDefaultSpacing(Styles? styles)
	{
		var spacingElement = GetDocDefaultSpacingElement(styles);
		if (spacingElement is null)
		{
			return ParagraphSpacing.None;
		}

		return new ParagraphSpacing(
			ParseTwips(spacingElement.Before?.Value),
			ParseTwips(spacingElement.After?.Value),
			ParseTwips(spacingElement.Line?.Value),
			MapLineRule(spacingElement.LineRule?.Value, null));
	}

	private static SpacingBetweenLines? GetDocDefaultSpacingElement(Styles? styles)
		=> styles?.DocDefaults?.ParagraphPropertiesDefault?.ParagraphPropertiesBaseStyle?.SpacingBetweenLines;
	/// <summary>
	/// Parses a twip string value, returning 0 if null or invalid.
	/// </summary>
	private static float ParseTwips(string? value)
		=> value is not null && float.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out var result)
			? result
			: 0f;
}
