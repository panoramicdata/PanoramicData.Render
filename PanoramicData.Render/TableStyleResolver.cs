namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Resolves table style properties and conditional formatting bands.
/// </summary>
internal static class TableStyleResolver
{
	/// <summary>
	/// Resolves a table style by ID and applies conditional style overrides in order.
	/// Walks the <c>basedOn</c> style chain so that conditional formatting defined in
	/// ancestor styles (e.g. band row shading) is included.
	/// </summary>
	/// <param name="styles">The styles element containing table style definitions.</param>
	/// <param name="styleId">The table style ID.</param>
	/// <param name="conditionals">Conditional style types to apply in order.</param>
	/// <returns>The resolved table style, or <see langword="null"/> if no matching table style exists.</returns>
	public static ResolvedTableStyle? Resolve(
		Styles? styles,
		string styleId,
		IReadOnlyList<TableStyleOverrideValues>? conditionals)
	{
		if (string.IsNullOrWhiteSpace(styleId))
		{
			return null;
		}

		// Collect style chain from leaf → root via basedOn.
		var chain = CollectStyleChain(styles, styleId);
		if (chain.Count == 0)
		{
			return null;
		}

		// Reverse to root → leaf so derived styles override base styles.
		chain.Reverse();

		// Apply base (non-conditional) properties from root → leaf.
		var resolved = new MutableTableStyle();
		foreach (var style in chain)
		{
			if (style.StyleTableProperties is not null)
			{
				resolved.TableProperties = Clone(style.StyleTableProperties);
			}

			if (style.StyleParagraphProperties is not null)
			{
				resolved.ParagraphProperties = Clone(style.StyleParagraphProperties);
			}

			if (style.StyleRunProperties is not null)
			{
				resolved.RunProperties = Clone(style.StyleRunProperties);
			}
		}

		// Apply conditional overrides from root → leaf (leaf wins).
		var applied = new List<TableStyleOverrideValues>();
		foreach (var conditional in conditionals ?? [])
		{
			foreach (var style in chain)
			{
				var conditionalProps = style.Elements<TableStyleProperties>()
					.FirstOrDefault(p => p.Type?.Value == conditional);
				if (conditionalProps is null)
				{
					continue;
				}

				if (!applied.Contains(conditional))
				{
					applied.Add(conditional);
				}

				resolved.Apply(conditionalProps);
			}
		}

		return new ResolvedTableStyle
		{
			StyleId = styleId,
			TableProperties = resolved.TableProperties,
			TableRowProperties = resolved.TableRowProperties,
			TableCellProperties = resolved.TableCellProperties,
			ParagraphProperties = resolved.ParagraphProperties,
			RunProperties = resolved.RunProperties,
			AppliedConditionals = applied
		};
	}

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
			var style = styles?.Elements<Style>()
				.FirstOrDefault(s => s.Type?.Value == StyleValues.Table && s.StyleId?.Value == currentId);
			if (style is null)
			{
				break;
			}

			chain.Add(style);
			currentId = style.BasedOn?.Val?.Value;
		}

		return chain;
	}

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
		var shading = resolved?.TableCellProperties?.GetFirstChild<Shading>();

		return TableParser.ParseShading(shading);
	}

	internal static IReadOnlyList<TableStyleOverrideValues> GetCellConditionals(
		TableLookOptions look,
		int rowIndex,
		int columnIndex,
		int rowSpan,
		int columnSpan,
		int rowCount,
		int columnCount)
	{
		var conditionals = new List<TableStyleOverrideValues>();
		var isFirstRow = rowIndex == 0;
		var isLastRow = rowIndex + rowSpan >= rowCount;
		var isFirstColumn = columnIndex == 0;
		var isLastColumn = columnIndex + columnSpan >= columnCount;

		if (look.ApplyBandedRows)
		{
			var bodyStart = look.ApplyFirstRow ? 1 : 0;
			var bodyEndExclusive = look.ApplyLastRow ? rowCount - 1 : rowCount;
			if (rowIndex >= bodyStart && rowIndex < bodyEndExclusive)
			{
				var bandRowIndex = rowIndex - bodyStart;
				conditionals.Add(bandRowIndex % 2 == 0
					? TableStyleOverrideValues.Band1Horizontal
					: TableStyleOverrideValues.Band2Horizontal);
			}
		}

		if (look.ApplyBandedColumns)
		{
			var bodyStart = look.ApplyFirstColumn ? 1 : 0;
			var bodyEndExclusive = look.ApplyLastColumn ? columnCount - 1 : columnCount;
			if (columnIndex >= bodyStart && columnIndex < bodyEndExclusive)
			{
				var bandColumnIndex = columnIndex - bodyStart;
				conditionals.Add(bandColumnIndex % 2 == 0
					? TableStyleOverrideValues.Band1Vertical
					: TableStyleOverrideValues.Band2Vertical);
			}
		}

		if (look.ApplyFirstRow && isFirstRow)
		{
			conditionals.Add(TableStyleOverrideValues.FirstRow);
		}

		if (look.ApplyLastRow && isLastRow)
		{
			conditionals.Add(TableStyleOverrideValues.LastRow);
		}

		if (look.ApplyFirstColumn && isFirstColumn)
		{
			conditionals.Add(TableStyleOverrideValues.FirstColumn);
		}

		if (look.ApplyLastColumn && isLastColumn)
		{
			conditionals.Add(TableStyleOverrideValues.LastColumn);
		}

		return conditionals;
	}

	private static OpenXmlCompositeElement? Clone(OpenXmlCompositeElement? element)
	{
		return element is null ? null : (OpenXmlCompositeElement)element.CloneNode(true);
	}

	private sealed class MutableTableStyle
	{
		public OpenXmlCompositeElement? TableProperties { get; set; }
		public OpenXmlCompositeElement? TableRowProperties { get; set; }
		public OpenXmlCompositeElement? TableCellProperties { get; set; }
		public OpenXmlCompositeElement? ParagraphProperties { get; set; }
		public OpenXmlCompositeElement? RunProperties { get; set; }

		public void Apply(TableStyleProperties properties)
		{
			foreach (var child in properties.ChildElements.OfType<OpenXmlCompositeElement>())
			{
				switch (child.LocalName)
				{
					case "tblPr":
						TableProperties = Clone(child) ?? TableProperties;
						break;

					case "trPr":
						TableRowProperties = Clone(child) ?? TableRowProperties;
						break;

					case "tcPr":
						TableCellProperties = Clone(child) ?? TableCellProperties;
						break;

					case "pPr":
						ParagraphProperties = Clone(child) ?? ParagraphProperties;
						break;

					case "rPr":
						RunProperties = Clone(child) ?? RunProperties;
						break;
				}
			}
		}
	}
}
