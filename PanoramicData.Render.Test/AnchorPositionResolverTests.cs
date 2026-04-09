namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;

public sealed class AnchorPositionResolverTests
{
	[Fact]
	public void ResolveAbsolutePosition_PageRelativeWithOffsets_ComputesAbsoluteCoordinates()
	{
		var anchor = new AnchorImageRunElement
		{
			RelationshipId = "rId1",
			WidthEmu = 914400,
			HeightEmu = 914400,
			HorizontalRelativeFrom = AnchorRelativeFrom.Page,
			VerticalRelativeFrom = AnchorRelativeFrom.Page,
			HorizontalOffsetEmu = 182880,
			VerticalOffsetEmu = 274320
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

		var position = AnchorPositionResolver.ResolveAbsolutePosition(anchor, section, paragraphXTwips: 2000f, paragraphYTwips: 3000f, paragraphWidthTwips: 5000f);

		position.X.Should().BeApproximately(288f, 0.001f);
		position.Y.Should().BeApproximately(432f, 0.001f);
	}

	[Fact]
	public void ResolveAbsolutePosition_MarginRelativeWithCenterAlignment_CentersWithinContentArea()
	{
		var anchor = new AnchorImageRunElement
		{
			RelationshipId = "rId1",
			WidthEmu = 914400,
			HeightEmu = 914400,
			HorizontalRelativeFrom = AnchorRelativeFrom.Margin,
			VerticalRelativeFrom = AnchorRelativeFrom.Margin,
			HorizontalAlignment = AnchorAlignment.Center,
			VerticalAlignment = AnchorAlignment.Center
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

		var position = AnchorPositionResolver.ResolveAbsolutePosition(anchor, section, paragraphXTwips: 2000f, paragraphYTwips: 3000f, paragraphWidthTwips: 5000f);

		// X: 1in margin + ((6.5in content - 1in image) / 2) = 5400 twips
		position.X.Should().BeApproximately(5400f, 0.001f);
		// Y: 1in margin + ((9in content - 1in image) / 2) = 7200 twips
		position.Y.Should().BeApproximately(7200f, 0.001f);
	}

	[Fact]
	public void ResolveAbsolutePosition_ParagraphRelativeWithOffsets_UsesParagraphAnchorOrigin()
	{
		var anchor = new AnchorImageRunElement
		{
			RelationshipId = "rId1",
			WidthEmu = 457200,
			HeightEmu = 457200,
			HorizontalRelativeFrom = AnchorRelativeFrom.Paragraph,
			VerticalRelativeFrom = AnchorRelativeFrom.Paragraph,
			HorizontalOffsetEmu = 6350,
			VerticalOffsetEmu = -12700
		};
		var section = new SectionInfo();

		var position = AnchorPositionResolver.ResolveAbsolutePosition(anchor, section, paragraphXTwips: 2500f, paragraphYTwips: 5000f, paragraphWidthTwips: 4000f);

		// +10 twips and -20 twips offsets
		position.X.Should().BeApproximately(2510f, 0.001f);
		position.Y.Should().BeApproximately(4980f, 0.001f);
	}

	[Theory]
	[InlineData(3)]
	[InlineData(4)]
	public void ResolveAbsolutePosition_ColumnAndCharacterReferences_UseParagraphCoordinates(int horizontalReference)
	{
		var anchor = new AnchorImageRunElement
		{
			RelationshipId = "rId1",
			WidthEmu = 914400,
			HeightEmu = 914400,
			HorizontalRelativeFrom = (AnchorRelativeFrom)horizontalReference,
			VerticalRelativeFrom = AnchorRelativeFrom.Line,
			HorizontalOffsetEmu = 6350,
			VerticalOffsetEmu = 12700
		};
		var section = new SectionInfo();

		var position = AnchorPositionResolver.ResolveAbsolutePosition(anchor, section, paragraphXTwips: 3000f, paragraphYTwips: 6000f, paragraphWidthTwips: 4500f);

		position.X.Should().BeApproximately(3010f, 0.001f);
		position.Y.Should().BeApproximately(6020f, 0.001f);
	}

	[Fact]
	public void ResolveAbsolutePosition_MarginEdgeReferences_ResolveAgainstMarginBands()
	{
		var anchor = new AnchorImageRunElement
		{
			RelationshipId = "rId1",
			WidthEmu = 457200,
			HeightEmu = 457200,
			HorizontalRelativeFrom = AnchorRelativeFrom.RightMargin,
			VerticalRelativeFrom = AnchorRelativeFrom.BottomMargin,
			HorizontalAlignment = AnchorAlignment.Right,
			VerticalAlignment = AnchorAlignment.Bottom
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

		var position = AnchorPositionResolver.ResolveAbsolutePosition(anchor, section, paragraphXTwips: 0f, paragraphYTwips: 0f, paragraphWidthTwips: 0f);

		// Right margin band starts at 10800 twips and is 1440 twips wide; image width is 720 twips.
		position.X.Should().BeApproximately(11520f, 0.001f);
		// Bottom margin band starts at 14400 twips and is 1440 twips high; image height is 720 twips.
		position.Y.Should().BeApproximately(15120f, 0.001f);
	}

	[Fact]
	public void ResolveAbsolutePosition_NullAnchor_ThrowsArgumentNullException()
	{
		var section = new SectionInfo();

		var act = () => AnchorPositionResolver.ResolveAbsolutePosition(null!, section, 0f, 0f, 0f);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void ResolveAbsolutePosition_NullSection_ThrowsArgumentNullException()
	{
		var anchor = new AnchorImageRunElement
		{
			RelationshipId = "rId1",
			WidthEmu = 914400,
			HeightEmu = 914400
		};

		var act = () => AnchorPositionResolver.ResolveAbsolutePosition(anchor, null!, 0f, 0f, 0f);

		act.Should().Throw<ArgumentNullException>();
	}
}
