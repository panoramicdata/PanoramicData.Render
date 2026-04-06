namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Represents resolved table style properties after applying conditional table style overrides.
/// </summary>
internal sealed class ResolvedTableStyle
{
	/// <summary>
	/// Gets the resolved table style ID.
	/// </summary>
	public required string StyleId { get; init; }

	/// <summary>
	/// Gets the resolved table properties fragment.
	/// </summary>
	public OpenXmlCompositeElement? TableProperties { get; init; }

	/// <summary>
	/// Gets the resolved table row properties fragment.
	/// </summary>
	public OpenXmlCompositeElement? TableRowProperties { get; init; }

	/// <summary>
	/// Gets the resolved table cell properties fragment.
	/// </summary>
	public OpenXmlCompositeElement? TableCellProperties { get; init; }

	/// <summary>
	/// Gets the resolved paragraph properties fragment.
	/// </summary>
	public OpenXmlCompositeElement? ParagraphProperties { get; init; }

	/// <summary>
	/// Gets the resolved run properties fragment.
	/// </summary>
	public OpenXmlCompositeElement? RunProperties { get; init; }

	/// <summary>
	/// Gets the conditional style types applied during resolution.
	/// </summary>
	public required IReadOnlyList<TableStyleOverrideValues> AppliedConditionals { get; init; }
}
