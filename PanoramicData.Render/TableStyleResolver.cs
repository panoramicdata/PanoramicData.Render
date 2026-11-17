namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Resolves table style properties and conditional formatting bands.
/// </summary>
internal static partial class TableStyleResolver
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
}
