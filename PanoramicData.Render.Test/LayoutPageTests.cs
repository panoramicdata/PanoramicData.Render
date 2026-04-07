namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;

public sealed class LayoutPageTests
{
	[Fact]
	public void Properties_ReturnAssignedValues()
	{
		var section = new SectionInfo { PageWidth = 9000 };
		var blocks = new LayoutBlock[] { };
		var page = new LayoutPage
		{
			Section = section,
			PageNumber = 3,
			Blocks = blocks
		};

		page.Section.Should().BeSameAs(section);
		page.PageNumber.Should().Be(3);
		page.Blocks.Should().BeSameAs(blocks);
	}
}
