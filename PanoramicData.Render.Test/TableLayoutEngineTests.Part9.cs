namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using static PanoramicData.Render.Test.NestedTableBuilders;
using static PanoramicData.Render.Test.TableLayoutBuilders;

public sealed partial class TableLayoutEngineTests
{
	[Fact]
	public void Layout_FullPipeline_ExactHeightIgnoresMarginsAndContent()
	{
		var margins = new CellMargins(200f, 50f, 200f, 50f);
		var cell = new TableCellElement
		{
			Blocks = [new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() },
					  new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() },
					  new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() }],
			Margins = margins,
		};
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(4800f)],
			Rows = [new TableRowElement
			{
				Cells = [cell],
				HeightTwips = 300f,
				HeightRule = RowHeightRule.Exact,
			}],
		};

		var layout = TableLayoutEngine.Layout(table, 9600f);

		// Exact ignores content (200+720+200=1120) and uses specified 300
		layout.RowHeights[0].Should().Be(300f);
		layout.TotalHeightTwips.Should().Be(300f);
	}
}
