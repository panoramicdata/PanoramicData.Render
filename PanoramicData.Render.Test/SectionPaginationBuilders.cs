namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;

internal static class SectionPaginationBuilders
{
	internal static IReadOnlyList<LayoutPage> Paginate(SectionInfo bodySection, params LayoutBlock[] blocks)
		=> PageBuilder.PaginateDocument(blocks, bodySection);

	internal static void AssertPageNumbers(IReadOnlyList<LayoutPage> pages, params int[] expected)
		=> pages.Select(page => page.PageNumber).Should().Equal(expected);

	internal static LayoutBlock MakeBlock(float heightTwips, bool keepWithNext = false)
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		return new LayoutBlock(para, heightTwips, KeepWithNext: keepWithNext);
	}

	internal static LayoutBlock MakeSplittableBlock(float[] lineHeights)
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		return new LayoutBlock(para, lineHeights.Sum(), 0f, 0f, lineHeights, WidowOrphanControl: false);
	}

	internal static LayoutBlock MakeSectionBreak(SectionInfo sectionInfo) =>
		new(new SectionBreakBlock { SectionInfo = sectionInfo }, 0f);
}
