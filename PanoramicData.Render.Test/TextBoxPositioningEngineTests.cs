namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;

public sealed class TextBoxPositioningEngineTests
{
	[Fact]
	public void Position_NullTextFrame_ThrowsArgumentNullException()
	{
		var placement = new AnchorPlacementInfo();
		var section = new SectionInfo();

		var act = () => TextBoxPositioningEngine.Position(null!, placement, 914400, 457200, section, 0f, 0f, 0f);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("textFrame");
	}

	[Fact]
	public void Position_NonPositiveWidth_ThrowsArgumentOutOfRangeException()
	{
		var textFrame = new ShapeTextFrameInfo { HasTextFrame = true, Text = "Hello" };
		var placement = new AnchorPlacementInfo();
		var section = new SectionInfo();

		var act = () => TextBoxPositioningEngine.Position(textFrame, placement, 0, 457200, section, 0f, 0f, 0f);

		act.Should().Throw<ArgumentOutOfRangeException>()
			.WithParameterName("widthEmu");
	}

	[Fact]
	public void Position_ComputesAbsoluteLocationAndLayoutsContent()
	{
		var textFrame = new ShapeTextFrameInfo
		{
			HasTextFrame = true,
			Text = "A long enough sentence to wrap inside the text box"
		};
		var placement = new AnchorPlacementInfo
		{
			HorizontalRelativeFrom = AnchorRelativeFrom.Margin,
			VerticalRelativeFrom = AnchorRelativeFrom.Paragraph,
			HorizontalAlignment = AnchorAlignment.Center,
			VerticalOffsetEmu = 12700,
			WrapStyle = AnchorWrapStyle.Square
		};
		var section = new SectionInfo
		{
			PageWidth = 12240,
			PageHeight = 15840,
			MarginLeft = 1440,
			MarginRight = 1440,
			MarginTop = 1440,
			MarginBottom = 1440
		};

		var positioned = TextBoxPositioningEngine.Position(
			textFrame,
			placement,
			widthEmu: 914400,
			heightEmu: 457200,
			section,
			paragraphXTwips: 2500f,
			paragraphYTwips: 3000f,
			paragraphWidthTwips: 4000f,
			fontFamily: "Arial");

		positioned.XTwips.Should().BeApproximately(5400f, 0.001f);
		positioned.YTwips.Should().BeApproximately(3020f, 0.001f);
		positioned.WidthTwips.Should().BeApproximately(1440f, 0.001f);
		positioned.HeightTwips.Should().BeApproximately(720f, 0.001f);
		positioned.Blocks.Should().NotBeEmpty();
		positioned.ContentHeightTwips.Should().BeGreaterThan(TextBoxLayoutEngine.DefaultLineHeightTwips);
		positioned.AnchorPlacement.WrapStyle.Should().Be(AnchorWrapStyle.Square);
	}
}