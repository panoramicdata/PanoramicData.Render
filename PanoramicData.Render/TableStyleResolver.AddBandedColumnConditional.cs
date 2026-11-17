namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class TableStyleResolver
{
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
}
