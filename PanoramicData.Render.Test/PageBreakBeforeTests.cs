namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;

public sealed class PageBreakBeforeTests
{
	[Fact]
	public void Parse_ParagraphWithPageBreakBefore_SetsProperty()
		=> AssertPageBreakBefore(TestDocxBuilder.CreateDocxWithPageBreakBefore(), 1, expected: true);

	[Fact]
	public void Parse_ParagraphWithoutPageBreakBefore_PropertyIsFalse()
		=> AssertPageBreakBefore(TestDocxBuilder.CreateMinimalDocx(), 0, expected: false);

	[Fact]
	public void Parse_PageBreakBeforeWithValFalse_PropertyIsFalse()
		=> AssertPageBreakBefore(TestDocxBuilder.CreateDocxWithPageBreakBeforeDisabled(), 0, expected: false);

	[Fact]
	public void Parse_FirstParagraphWithoutBreak_NoPageBreak()
		=> AssertPageBreakBefore(TestDocxBuilder.CreateDocxWithPageBreakBefore(), 0, expected: false);

	private static void AssertPageBreakBefore(Stream docx, int blockIndex, bool expected)
	{
		using var stream = docx;
		using var doc = DocxDocument.Load(stream);

		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);

		var para = blocks[blockIndex].Should().BeOfType<ParagraphBlock>().Subject;
		para.PageBreakBefore.Should().Be(expected);
	}
}