namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class TableParser
{
	internal static CellTextDirection ParseCellTextDirection(TextDirection? textDir)
	{
		if (textDir?.Val?.Value is null)
		{
			return CellTextDirection.LeftToRightTopToBottom;
		}

		if (textDir.Val.Value == TextDirectionValues.TopToBottomRightToLeft
			|| textDir.Val.Value == TextDirectionValues.TopToBottomRightToLeftRotated)
		{
			return CellTextDirection.TopToBottomRightToLeft;
		}

		if (textDir.Val.Value == TextDirectionValues.BottomToTopLeftToRight
			|| textDir.Val.Value == TextDirectionValues.BottomToTopLeftToRight2010)
		{
			return CellTextDirection.BottomToTopLeftToRight;
		}

		return CellTextDirection.LeftToRightTopToBottom;
	}

	internal static CellMargins ParseCellMargins(TableCellMargin? margins)
	{
		if (margins is null)
		{
			return CellMargins.None;
		}

		return new CellMargins(
			ParseMarginWidth(margins.TopMargin),
			ParseMarginWidth(margins.RightMargin),
			ParseMarginWidth(margins.BottomMargin),
			ParseMarginWidth(margins.LeftMargin));
	}

	internal static CellMargins ParseDefaultCellMargins(TableCellMarginDefault? margins)
	{
		if (margins is null)
		{
			return CellMargins.None;
		}

		return new CellMargins(
			ParseTableWidthTypeTwips(margins.TopMargin),
			ParseTableCellMarginWidth(margins.TableCellRightMargin),
			ParseTableWidthTypeTwips(margins.BottomMargin),
			ParseTableCellMarginWidth(margins.TableCellLeftMargin));
	}

	private static float ParseTableWidthTypeTwips(TableWidthType? widthType)
	{
		if (widthType?.Width?.Value is { } w && float.TryParse(w, out var parsed))
		{
			return parsed;
		}

		return 0f;
	}

	private static float ParseTableCellMarginWidth(OpenXmlElement? marginElement)
	{
		if (marginElement is null)
		{
			return 0f;
		}

		var widthAttr = marginElement.GetAttributes()
			.FirstOrDefault(a => a.LocalName == "w" && a.NamespaceUri == WordprocessingNamespace);
		if (widthAttr.Value is { } w && float.TryParse(w, out var parsed))
		{
			return parsed;
		}

		return 0f;
	}
}
