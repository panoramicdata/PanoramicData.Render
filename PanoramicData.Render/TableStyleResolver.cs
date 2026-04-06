namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Resolves table style properties and conditional formatting bands.
/// </summary>
internal static class TableStyleResolver
{
	/// <summary>
	/// Resolves a table style by ID and applies conditional style overrides in order.
	/// </summary>
	/// <param name="stylesPart">The styles part.</param>
	/// <param name="styleId">The table style ID.</param>
	/// <param name="conditionals">Conditional style types to apply in order.</param>
	/// <returns>The resolved table style, or <see langword="null"/> if no matching table style exists.</returns>
	public static ResolvedTableStyle? Resolve(
		StyleDefinitionsPart? stylesPart,
		string styleId,
		IReadOnlyList<TableStyleOverrideValues>? conditionals)
	{
		if (string.IsNullOrWhiteSpace(styleId))
		{
			return null;
		}

		var style = stylesPart?.Styles?.Elements<Style>()
			.FirstOrDefault(s => s.Type?.Value == StyleValues.Table && s.StyleId?.Value == styleId);
		if (style is null)
		{
			return null;
		}

		var resolved = new MutableTableStyle
		{
			TableProperties = Clone(style.StyleTableProperties),
			TableRowProperties = null,
			TableCellProperties = null,
			ParagraphProperties = Clone(style.StyleParagraphProperties),
			RunProperties = Clone(style.StyleRunProperties)
		};

		var applied = new List<TableStyleOverrideValues>();
		foreach (var conditional in conditionals ?? [])
		{
			var conditionalProps = style.Elements<TableStyleProperties>()
				.FirstOrDefault(p => p.Type?.Value == conditional);
			if (conditionalProps is null)
			{
				continue;
			}

			applied.Add(conditional);
			resolved.Apply(conditionalProps);
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
