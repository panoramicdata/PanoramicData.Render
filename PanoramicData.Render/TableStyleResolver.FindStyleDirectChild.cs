namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class TableStyleResolver
{
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
