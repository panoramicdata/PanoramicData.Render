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
			ApplyBaseProperties(resolved, style);
		}

		// Apply conditional overrides from root → leaf (leaf wins).
		var applied = ApplyConditionals(resolved, chain, conditionals);

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

	private static void ApplyBaseProperties(MutableTableStyle resolved, Style style)
	{
		if (style.StyleTableProperties is not null)
		{
			resolved.TableProperties = Clone(style.StyleTableProperties);
		}

		// Collect whole-table cell and row properties (e.g. default background colour inherited from
		// GridTable5Dark-Accent2, so unbanded rows get their base shading). Local-name matching is used
		// because some SDK versions store these as OpenXmlUnknownElement rather than the typed properties.
		var baseTcPr = FindStyleDirectChild(style, "tcPr");
		if (baseTcPr is not null)
		{
			resolved.TableCellProperties = baseTcPr;
		}

		var baseTrPr = FindStyleDirectChild(style, "trPr");
		if (baseTrPr is not null)
		{
			resolved.TableRowProperties = baseTrPr;
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

	private static List<TableStyleOverrideValues> ApplyConditionals(MutableTableStyle resolved, List<Style> chain, IReadOnlyList<TableStyleOverrideValues>? conditionals)
	{
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

		return applied;
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

	private static IReadOnlyList<TableStyleOverrideValues>? GetBand2FallbackConditionals(IReadOnlyList<TableStyleOverrideValues> conditionals)
	{
		var changed = false;
		var fallback = new List<TableStyleOverrideValues>(conditionals.Count);
		for (var i = 0; i < conditionals.Count; i++)
		{
			var conditional = conditionals[i];
			if (conditional == TableStyleOverrideValues.Band2Horizontal)
			{
				conditional = TableStyleOverrideValues.Band1Horizontal;
			}
			else if (conditional == TableStyleOverrideValues.Band2Vertical)
			{
				conditional = TableStyleOverrideValues.Band1Vertical;
			}

			changed |= conditional != conditionals[i];
			fallback.Add(conditional);
		}

		if (!changed)
		{
			return null;
		}

		return fallback;
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
		AddBandedRowConditional(conditionals, look, rowIndex, rowCount);
		AddBandedColumnConditional(conditionals, look, columnIndex, columnCount);

		if (look.ApplyFirstRow && rowIndex == 0)
		{
			conditionals.Add(TableStyleOverrideValues.FirstRow);
		}

		if (look.ApplyLastRow && rowIndex + rowSpan >= rowCount)
		{
			conditionals.Add(TableStyleOverrideValues.LastRow);
		}

		if (look.ApplyFirstColumn && columnIndex == 0)
		{
			conditionals.Add(TableStyleOverrideValues.FirstColumn);
		}

		if (look.ApplyLastColumn && columnIndex + columnSpan >= columnCount)
		{
			conditionals.Add(TableStyleOverrideValues.LastColumn);
		}

		return conditionals;
	}

	private static void AddBandedRowConditional(List<TableStyleOverrideValues> conditionals, TableLookOptions look, int rowIndex, int rowCount)
	{
		if (!look.ApplyBandedRows)
		{
			return;
		}

		var bodyStart = look.ApplyFirstRow ? 1 : 0;
		var bodyEndExclusive = look.ApplyLastRow ? rowCount - 1 : rowCount;
		if (rowIndex >= bodyStart && rowIndex < bodyEndExclusive)
		{
			conditionals.Add((rowIndex - bodyStart) % 2 == 0
				? TableStyleOverrideValues.Band1Horizontal
				: TableStyleOverrideValues.Band2Horizontal);
		}
	}

	private static void AddBandedColumnConditional(List<TableStyleOverrideValues> conditionals, TableLookOptions look, int columnIndex, int columnCount)
	{
		if (!look.ApplyBandedColumns)
		{
			return;
		}

		var bodyStart = look.ApplyFirstColumn ? 1 : 0;
		var bodyEndExclusive = look.ApplyLastColumn ? columnCount - 1 : columnCount;
		if (columnIndex >= bodyStart && columnIndex < bodyEndExclusive)
		{
			conditionals.Add((columnIndex - bodyStart) % 2 == 0
				? TableStyleOverrideValues.Band1Vertical
				: TableStyleOverrideValues.Band2Vertical);
		}
	}
	/// <summary>
	/// Resolves the default cell margins for a table style by walking the style chain.
	/// Returns Word's built-in default (108 twips left/right, 0 top/bottom) when no margins
	/// are defined anywhere in the chain.
	/// </summary>
	/// <param name="styles">The styles element containing table style definitions.</param>
	/// <param name="styleId">The table style ID.</param>
	/// <returns>The resolved default cell margins.</returns>
	public static CellMargins ResolveDefaultCellMargins(Styles? styles, string? styleId)
	{
		if (string.IsNullOrWhiteSpace(styleId))
		{
			return new CellMargins(Top: 0f, Right: 108f, Bottom: 0f, Left: 108f);
		}

		var chain = CollectStyleChain(styles, styleId);
		chain.Reverse(); // root → leaf; leaf wins

		foreach (var style in chain)
		{
			var tblPr = style.StyleTableProperties;
			if (tblPr is null)
			{
				continue;
			}

			var tblCellMar = tblPr.GetFirstChild<TableCellMarginDefault>();
			if (tblCellMar is null)
			{
				continue;
			}

			var margins = TableParser.ParseDefaultCellMargins(tblCellMar);
			if (margins != CellMargins.None)
			{
				return margins;
			}
		}

		// Word built-in default: 108 twips left/right, 0 top/bottom (from the TableNormal style).
		return new CellMargins(Top: 0f, Right: 108f, Bottom: 0f, Left: 108f);
	}

	private static OpenXmlCompositeElement? Clone(OpenXmlCompositeElement? element)
	{
		return element is null ? null : (OpenXmlCompositeElement)element.CloneNode(true);
	}

	/// <summary>
	/// Finds and clones a direct child element of a <see cref="Style"/> element by local name.
	/// Uses local-name matching rather than typed access because the SDK may store
	/// <c>w:tcPr</c> and <c>w:trPr</c> as <see cref="OpenXmlUnknownElement"/> rather than their
	/// strongly-typed counterparts when they appear directly inside a <c>w:style</c> element.
	/// </summary>
	private static OpenXmlCompositeElement? FindStyleDirectChild(Style style, string localName)
	{
		foreach (var child in style.ChildElements)
		{
			if (child.LocalName != localName)
			{
				continue;
			}

			if (child is OpenXmlCompositeElement composite)
			{
				return (OpenXmlCompositeElement)composite.CloneNode(true);
			}

			// Element stored as an unrecognised type (e.g. OpenXmlUnknownElement).
			// Reconstruct as the appropriate strongly-typed container and transfer children.
			OpenXmlCompositeElement reconstructed = localName == "tcPr"
				? new TableCellProperties()
				: new TableRowProperties();
			foreach (var grandChild in child.ChildElements)
			{
				reconstructed.AppendChild(grandChild.CloneNode(true));
			}

			return reconstructed;
		}

		return null;
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
				if (child.LocalName == "tblPr")
				{
					TableProperties = CloneOr(child, TableProperties);
				}
				else if (child.LocalName == "trPr")
				{
					TableRowProperties = CloneOr(child, TableRowProperties);
				}
				else if (child.LocalName == "tcPr")
				{
					TableCellProperties = CloneOr(child, TableCellProperties);
				}
				else if (child.LocalName == "pPr")
				{
					ParagraphProperties = CloneOr(child, ParagraphProperties);
				}
				else if (child.LocalName == "rPr")
				{
					RunProperties = CloneOr(child, RunProperties);
				}
			}
		}

		private static OpenXmlCompositeElement? CloneOr(OpenXmlCompositeElement child, OpenXmlCompositeElement? fallback)
			=> Clone(child) ?? fallback;
	}
}