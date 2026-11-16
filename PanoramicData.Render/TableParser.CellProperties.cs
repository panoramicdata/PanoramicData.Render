namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class TableParser
{
	private static int ParseGridSpan(TableCellProperties? tcPr)
	{
		if (tcPr?.GridSpan?.Val?.Value is { } gs && gs > 0)
		{
			return gs;
		}

		return 1;
	}

	private static VerticalMergeState ParseVerticalMerge(TableCellProperties? tcPr)
	{
		var vMerge = tcPr?.VerticalMerge;
		if (vMerge is null)
		{
			return VerticalMergeState.None;
		}

		// w:vMerge val="restart" → Restart; w:vMerge (no val or val="continue") → Continue
		if (vMerge.Val?.Value == MergedCellValues.Restart)
		{
			return VerticalMergeState.Restart;
		}

		return VerticalMergeState.Continue;
	}

	internal static CellVerticalAlignment ParseCellVerticalAlignment(TableCellVerticalAlignment? vAlign)
	{
		if (vAlign?.Val?.Value is null)
		{
			return CellVerticalAlignment.Top;
		}

		if (vAlign.Val.Value == TableVerticalAlignmentValues.Center)
		{
			return CellVerticalAlignment.Center;
		}

		if (vAlign.Val.Value == TableVerticalAlignmentValues.Bottom)
		{
			return CellVerticalAlignment.Bottom;
		}

		return CellVerticalAlignment.Top;
	}

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

	private static float ParseMarginWidth(TopMargin? margin)
	{
		if (margin?.Width?.Value is { } w && float.TryParse(w, out var parsed))
		{
			return parsed;
		}

		return 0f;
	}

	private static float ParseMarginWidth(RightMargin? margin)
	{
		if (margin?.Width?.Value is { } w && float.TryParse(w, out var parsed))
		{
			return parsed;
		}

		return 0f;
	}

	private static float ParseMarginWidth(BottomMargin? margin)
	{
		if (margin?.Width?.Value is { } w && float.TryParse(w, out var parsed))
		{
			return parsed;
		}

		return 0f;
	}

	private static float ParseMarginWidth(LeftMargin? margin)
	{
		if (margin?.Width?.Value is { } w && float.TryParse(w, out var parsed))
		{
			return parsed;
		}

		return 0f;
	}
}
