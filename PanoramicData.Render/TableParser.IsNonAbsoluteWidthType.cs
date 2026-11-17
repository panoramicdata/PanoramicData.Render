namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class TableParser
{
	private static bool IsNonAbsoluteWidthType(TableWidthUnitValues? type)
		=> type == TableWidthUnitValues.Auto || type == TableWidthUnitValues.Nil || type == TableWidthUnitValues.Pct;
	internal static TableLookOptions ParseTableLook(TableLook? tableLook)
	{
		if (tableLook is null)
		{
			return TableLookOptions.None;
		}

		return new TableLookOptions(
			ApplyFirstRow: ParseOnOffAttribute(tableLook, "firstRow"),
			ApplyLastRow: ParseOnOffAttribute(tableLook, "lastRow"),
			ApplyFirstColumn: ParseOnOffAttribute(tableLook, "firstColumn"),
			ApplyLastColumn: ParseOnOffAttribute(tableLook, "lastColumn"),
			ApplyBandedRows: !ParseOnOffAttribute(tableLook, "noHBand"),
			ApplyBandedColumns: !ParseOnOffAttribute(tableLook, "noVBand"));
	}

	private static bool ParseOnOffAttribute(OpenXmlElement element, string localName)
	{
		var value = element.GetAttributes()
			.FirstOrDefault(attribute => attribute.LocalName == localName && attribute.NamespaceUri == WordprocessingNamespace)
			.Value;
		if (string.IsNullOrWhiteSpace(value))
		{
			return false;
		}

		return value.Equals("1", StringComparison.Ordinal)
			|| value.Equals("true", StringComparison.OrdinalIgnoreCase)
			|| value.Equals("on", StringComparison.OrdinalIgnoreCase);
	}

	internal static TableWidthValue ParseTableWidth(TableWidthType? tableWidth)
	{
		if (tableWidth is null)
		{
			return TableWidthValue.Auto;
		}

		var type = ParseWidthType(tableWidth.Type?.Value);
		var value = 0f;
		if (tableWidth.Width?.Value is { } w && float.TryParse(w, out var parsed))
		{
			value = parsed;
		}

		return new TableWidthValue(value, type);
	}

	private static TableWidthUnit ParseWidthType(TableWidthUnitValues? unit)
	{
		if (unit is null)
		{
			return TableWidthUnit.Auto;
		}

		if (unit == TableWidthUnitValues.Dxa)
		{
			return TableWidthUnit.Dxa;
		}

		if (unit == TableWidthUnitValues.Pct)
		{
			return TableWidthUnit.Pct;
		}

		if (unit == TableWidthUnitValues.Nil)
		{
			return TableWidthUnit.Nil;
		}

		return TableWidthUnit.Auto;
	}
}
