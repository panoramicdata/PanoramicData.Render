namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class TableParser
{
	private static TableCellElement ParseCell(TableCell tc, CellMargins defaultCellMargins)
	{
		var tcPr = tc.TableCellProperties;
		var cellMargins = ParseCellMargins(tcPr?.TableCellMargin);

		return new TableCellElement
		{
			Blocks = ParseCellContent(tc),
			GridSpan = ParseGridSpan(tcPr),
			VerticalMerge = ParseVerticalMerge(tcPr),
			Width = ParseTableWidth(tcPr?.TableCellWidth),
			VerticalAlignment = ParseCellVerticalAlignment(tcPr?.TableCellVerticalAlignment),
			TextDirection = ParseCellTextDirection(tcPr?.TextDirection),

			// Fall back to table-level default cell margins when the cell has none.
			Margins = cellMargins == CellMargins.None ? defaultCellMargins : cellMargins,
			Borders = ParseCellBorders(tcPr?.TableCellBorders),
			Shading = ParseShading(tcPr?.Shading),
		};
	}
	private static IReadOnlyList<DocumentBlock> ParseCellContent(TableCell cell)
	{
		var blocks = new List<DocumentBlock>();
		ParseCellElements(cell.ChildElements, blocks);
		return blocks;
	}

	private static void ParseCellElements(DocumentFormat.OpenXml.OpenXmlElementList elements, List<DocumentBlock> blocks)
	{
		foreach (var element in elements)
		{
			if (element is Paragraph paragraph)
			{
				blocks.Add(DocumentBlockParser.CreateParagraphBlock(paragraph));
			}
			else if (element is Table nestedTable)
			{
				blocks.Add(new TablePlaceholderBlock { TableElement = nestedTable });
			}
			else if (element is SdtBlock sdtBlock)
			{
				// Unwrap block-level content controls inside cells
				var sdtContent = sdtBlock.SdtContentBlock;
				if (sdtContent is not null)
				{
					ParseCellElements(sdtContent.ChildElements, blocks);
				}
			}
		}
	}
}
