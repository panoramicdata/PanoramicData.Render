namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;

public sealed class PageBreakBeforeTests
{
	[Fact]
	public void Parse_ParagraphWithPageBreakBefore_SetsProperty()
	{
		using var stream = TestDocxBuilder.CreateDocxWithPageBreakBefore();
		using var doc = DocxDocument.Load(stream);

		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);

		var para = blocks[1].Should().BeOfType<ParagraphBlock>().Subject;
		para.PageBreakBefore.Should().BeTrue();
	}

	[Fact]
	public void Parse_ParagraphWithoutPageBreakBefore_PropertyIsFalse()
	{
		using var stream = TestDocxBuilder.CreateMinimalDocx();
		using var doc = DocxDocument.Load(stream);

		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);

		var para = blocks[0].Should().BeOfType<ParagraphBlock>().Subject;
		para.PageBreakBefore.Should().BeFalse();
	}

	[Fact]
	public void Parse_PageBreakBeforeWithValFalse_PropertyIsFalse()
	{
		using var stream = TestDocxBuilder.CreateDocxWithPageBreakBeforeDisabled();
		using var doc = DocxDocument.Load(stream);

		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);

		var para = blocks[0].Should().BeOfType<ParagraphBlock>().Subject;
		para.PageBreakBefore.Should().BeFalse();
	}

	[Fact]
	public void Parse_FirstParagraphWithoutBreak_NoPageBreak()
	{
		using var stream = TestDocxBuilder.CreateDocxWithPageBreakBefore();
		using var doc = DocxDocument.Load(stream);

		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);

		var firstPara = blocks[0].Should().BeOfType<ParagraphBlock>().Subject;
		firstPara.PageBreakBefore.Should().BeFalse();
	}
}
