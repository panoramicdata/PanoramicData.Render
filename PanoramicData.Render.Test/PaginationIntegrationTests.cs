using AwesomeAssertions;
using Xunit;

namespace PanoramicData.Render.Test;

/// <summary>
/// Cross-cutting integration tests verifying page break positions for documents
/// with known pagination behavior, exercising multiple PageBuilder features together.
/// </summary>
public sealed class PaginationIntegrationTests
{
	[Theory]
	[InlineData("h6000 l4x2000!f", "-,3|1")]
	[InlineData("h5000!n h5000!b h1000", "-|-,-")]
	[InlineData("h10000 h2000!n l3x500!f", "-|-,3")]
	[InlineData("h10000 l4x1000!kf", "-|4")]
	[InlineData("l10x1200/500/300!f", "10")]
	[InlineData("h800!nk l20x600!f h800!nk h2000", "-,20|-,-")]
	[InlineData("h500", "-")]
	[InlineData("h6000 h6000!n h6000!b l10x2000!f", "-,-|-,3|6|1")]
	public void Paginate_PlacesBlocksAsExpected(string spec, string expectedShape)
	{
		var result = PaginationTestKit.Paginate(spec);

		PaginationTestKit.Shape(result).Should().Be(expectedShape);
		result.Select(page => page.PageNumber).Should().Equal(Enumerable.Range(1, result.Count));
	}

	[Fact]
	public void EmptyDocument_NoPages()
	{
		var result = PageBuilder.Paginate([], new SectionInfo());

		result.Should().BeEmpty();
	}

	[Fact]
	public void CustomMargins_ReduceAvailableSpace()
	{
		var section = new SectionInfo { MarginTop = 3000, MarginBottom = 3000 };

		var result = PaginationTestKit.Paginate("h5000 h5000", section);

		PaginationTestKit.Shape(result).Should().Be("-|-");
	}

	[Fact]
	public void AllPagesReferenceCorrectSection()
	{
		var section = new SectionInfo { PageWidth = 9000 };

		var result = PaginationTestKit.Paginate("h7000 h7000 h7000", section);

		result.Should().OnlyContain(page => ReferenceEquals(page.Section, section));
	}
}
