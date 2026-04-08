namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed class FootnoteSplitterTests
{
	[Fact]
	public void Split_NullBlocks_ThrowsArgumentNullException()
	{
		var act = () => FootnoteSplitter.Split(null!, 1000f);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("blocks");
	}

	[Fact]
	public void Split_EmptyBlocks_ReturnsBothEmpty()
	{
		var (current, overflow) = FootnoteSplitter.Split([], 1000f);

		current.Should().BeEmpty();
		overflow.Should().BeEmpty();
	}

	[Fact]
	public void Split_AllFit_AllOnCurrentPage()
	{
		var blocks = new[]
		{
			MakeBlock(200f),
			MakeBlock(200f),
			MakeBlock(200f),
		};

		var (current, overflow) = FootnoteSplitter.Split(blocks, 1000f);

		current.Should().HaveCount(3);
		overflow.Should().BeEmpty();
	}

	[Fact]
	public void Split_NoneFit_AllOverflow()
	{
		var blocks = new[]
		{
			MakeBlock(500f),
			MakeBlock(500f),
		};

		var (current, overflow) = FootnoteSplitter.Split(blocks, 100f);

		current.Should().BeEmpty();
		overflow.Should().HaveCount(2);
	}

	[Fact]
	public void Split_PartialFit_SplitsCorrectly()
	{
		var blocks = new[]
		{
			MakeBlock(200f),
			MakeBlock(200f),
			MakeBlock(200f),
		};

		var (current, overflow) = FootnoteSplitter.Split(blocks, 450f);

		current.Should().HaveCount(2); // 200 + 200 = 400 ≤ 450
		overflow.Should().ContainSingle(); // 200 would be 600 > 450
	}

	[Fact]
	public void Split_ExactFit_AllOnCurrentPage()
	{
		var blocks = new[]
		{
			MakeBlock(200f),
			MakeBlock(200f),
		};

		var (current, overflow) = FootnoteSplitter.Split(blocks, 400f);

		current.Should().HaveCount(2);
		overflow.Should().BeEmpty();
	}

	[Fact]
	public void Split_ZeroAvailableHeight_AllOverflow()
	{
		var blocks = new[]
		{
			MakeBlock(200f),
		};

		var (current, overflow) = FootnoteSplitter.Split(blocks, 0f);

		current.Should().BeEmpty();
		overflow.Should().ContainSingle();
	}

	[Fact]
	public void Split_NegativeAvailableHeight_AllOverflow()
	{
		var blocks = new[]
		{
			MakeBlock(200f),
		};

		var (current, overflow) = FootnoteSplitter.Split(blocks, -100f);

		current.Should().BeEmpty();
		overflow.Should().ContainSingle();
	}

	[Fact]
	public void Split_SingleBlockFits_OnCurrentPage()
	{
		var blocks = new[] { MakeBlock(100f) };

		var (current, overflow) = FootnoteSplitter.Split(blocks, 200f);

		current.Should().ContainSingle();
		overflow.Should().BeEmpty();
	}

	[Fact]
	public void Split_SingleBlockTooLarge_AllOverflow()
	{
		var blocks = new[] { MakeBlock(500f) };

		var (current, overflow) = FootnoteSplitter.Split(blocks, 200f);

		current.Should().BeEmpty();
		overflow.Should().ContainSingle();
	}

	[Fact]
	public void Split_PreservesBlockIdentity()
	{
		var block1 = MakeBlock(200f);
		var block2 = MakeBlock(200f);
		var block3 = MakeBlock(200f);

		var (current, overflow) = FootnoteSplitter.Split([block1, block2, block3], 450f);

		current[0].Should().Be(block1);
		current[1].Should().Be(block2);
		overflow[0].Should().Be(block3);
	}

	private static LayoutBlock MakeBlock(float heightTwips)
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		return new LayoutBlock(para, heightTwips);
	}
}
