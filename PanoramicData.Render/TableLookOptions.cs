namespace PanoramicData.Render;

/// <summary>
/// Represents the enabled table-style conditional formatting regions from <c>w:tblLook</c>.
/// </summary>
internal readonly record struct TableLookOptions(
	bool ApplyFirstRow = false,
	bool ApplyLastRow = false,
	bool ApplyFirstColumn = false,
	bool ApplyLastColumn = false,
	bool ApplyBandedRows = false,
	bool ApplyBandedColumns = false)
{
	/// <summary>
	/// No conditional table-style formatting regions are enabled.
	/// </summary>
	public static readonly TableLookOptions None = new();
}