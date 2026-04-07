namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed class LayoutBlockTests
{
	[Fact]
	public void Constructor_SetsBlockAndHeight()
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		var block = new LayoutBlock(para, 500f);

		block.Block.Should().BeSameAs(para);
		block.HeightTwips.Should().Be(500f);
	}

	[Fact]
	public void Constructor_DefaultsOptionalProperties()
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		var block = new LayoutBlock(para, 500f);

		block.SpaceBefore.Should().Be(0f);
		block.SpaceAfter.Should().Be(0f);
		block.LineHeights.Should().BeNull();
	}

	[Fact]
	public void Constructor_SetsAllProperties()
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		var lineHeights = new[] { 200f, 200f };
		var block = new LayoutBlock(para, 500f, 50f, 50f, lineHeights);

		block.SpaceBefore.Should().Be(50f);
		block.SpaceAfter.Should().Be(50f);
		block.LineHeights.Should().BeSameAs(lineHeights);
	}

	[Fact]
	public void Equality_SameValues_AreEqual()
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		var a = new LayoutBlock(para, 300f);
		var b = new LayoutBlock(para, 300f);

		a.Should().Be(b);
	}

	[Fact]
	public void Equality_DifferentHeight_AreNotEqual()
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		var a = new LayoutBlock(para, 300f);
		var b = new LayoutBlock(para, 400f);

		a.Should().NotBe(b);
	}
}
