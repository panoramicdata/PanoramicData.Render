namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class TableStyleResolver
{
	/// <summary>
	/// Collects the table style chain from leaf to root via <c>basedOn</c>.
	/// </summary>
	private static List<Style> CollectStyleChain(Styles? styles, string styleId)
	{
		var chain = new List<Style>();
		var currentId = styleId;
		var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		while (!string.IsNullOrEmpty(currentId) && visited.Add(currentId))
		{
			var style = FindTableStyle(styles, currentId);
			if (style is null)
			{
				break;
			}

			chain.Add(style);
			currentId = style.BasedOn?.Val?.Value;
		}

		return chain;
	}

	private static Style? FindTableStyle(Styles? styles, string styleId)
		=> styles?.Elements<Style>().FirstOrDefault(s => s.Type?.Value == StyleValues.Table && s.StyleId?.Value == styleId);
	/// <summary>
	/// Resolves the effective table-style shading for a single cell position.
	/// Direct cell shading is not considered here; callers should apply it as an override.
	/// </summary>
	public static ParagraphShading ResolveCellShading(
		Styles? styles,
		TableElement table,
		int rowIndex,
		int columnIndex,
		int rowSpan,
		int columnSpan,
		int rowCount,
		int columnCount)
	{
		ArgumentNullException.ThrowIfNull(table);

		if (string.IsNullOrWhiteSpace(table.StyleId))
		{
			return ParagraphShading.None;
		}

		var conditionals = GetCellConditionals(table.Look, rowIndex, columnIndex, rowSpan, columnSpan, rowCount, columnCount);
		var resolved = Resolve(styles, table.StyleId, conditionals);
		var shading = TableParser.ParseShading(resolved?.TableCellProperties?.GetFirstChild<Shading>());

		if (shading.HasVisibleShading)
		{
			return shading;
		}

		var fallbackConditionals = GetBand2FallbackConditionals(conditionals);
		if (fallbackConditionals is null)
		{
			return ParagraphShading.None;
		}

		var fallbackResolved = Resolve(styles, table.StyleId, fallbackConditionals);
		return TableParser.ParseShading(fallbackResolved?.TableCellProperties?.GetFirstChild<Shading>());
	}
}
