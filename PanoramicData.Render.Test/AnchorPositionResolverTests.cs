namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;

public sealed class AnchorPositionResolverTests
{
	private static SectionInfo OneInchMargins() => new()
	{
		PageWidth = 12240,
		PageHeight = 15840,
		MarginLeft = 1440,
		MarginRight = 1440,
		MarginTop = 1440,
		MarginBottom = 1440
	};

	private static AnchorImageRunElement Anchor(long sizeEmu, AnchorPlacementInfo placement) => new()
	{
		RelationshipId = "rId1",
		WidthEmu = sizeEmu,
		HeightEmu = sizeEmu,
		Placement = placement
	};

	private static AnchorPlacementInfo Placement(
		AnchorRelativeFrom horizontal,
		AnchorRelativeFrom vertical,
		long offsetX = 0,
		long offsetY = 0,
		AnchorAlignment alignX = default,
		AnchorAlignment alignY = default) => new()
		{
			HorizontalRelativeFrom = horizontal,
			VerticalRelativeFrom = vertical,
			HorizontalOffsetEmu = offsetX,
			VerticalOffsetEmu = offsetY,
			HorizontalAlignment = alignX,
			VerticalAlignment = alignY
		};

	private static void AssertPosition(AnchorAbsolutePosition position, float x, float y)
	{
		position.X.Should().BeApproximately(x, 0.001f);
		position.Y.Should().BeApproximately(y, 0.001f);
	}

	[Fact]
	public void ResolveAbsolutePosition_PageRelativeWithOffsets_ComputesAbsoluteCoordinates()
	{
		var anchor = Anchor(914400, Placement(AnchorRelativeFrom.Page, AnchorRelativeFrom.Page, 182880, 274320));

		var position = AnchorPositionResolver.ResolveAbsolutePosition(anchor, OneInchMargins(), paragraphXTwips: 2000f, paragraphYTwips: 3000f, paragraphWidthTwips: 5000f);

		AssertPosition(position, 288f, 432f);
	}

	[Fact]
	public void ResolveAbsolutePosition_MarginRelativeWithCenterAlignment_CentersWithinContentArea()
	{
		var anchor = Anchor(
			914400,
			Placement(AnchorRelativeFrom.Margin, AnchorRelativeFrom.Margin, alignX: AnchorAlignment.Center, alignY: AnchorAlignment.Center));

		var position = AnchorPositionResolver.ResolveAbsolutePosition(anchor, OneInchMargins(), paragraphXTwips: 2000f, paragraphYTwips: 3000f, paragraphWidthTwips: 5000f);

		// X: 1in margin + ((6.5in content - 1in image) / 2) = 5400 twips; Y likewise with 9in content = 7200 twips
		AssertPosition(position, 5400f, 7200f);
	}

	[Fact]
	public void ResolveAbsolutePosition_ParagraphRelativeWithOffsets_UsesParagraphAnchorOrigin()
	{
		var anchor = Anchor(457200, Placement(AnchorRelativeFrom.Paragraph, AnchorRelativeFrom.Paragraph, 6350, -12700));

		var position = AnchorPositionResolver.ResolveAbsolutePosition(anchor, new SectionInfo(), paragraphXTwips: 2500f, paragraphYTwips: 5000f, paragraphWidthTwips: 4000f);

		// +10 twips and -20 twips offsets
		AssertPosition(position, 2510f, 4980f);
	}

	[Theory]
	[InlineData(3)]
	[InlineData(4)]
	public void ResolveAbsolutePosition_ColumnAndCharacterReferences_UseParagraphCoordinates(int horizontalReference)
	{
		var anchor = Anchor(914400, Placement((AnchorRelativeFrom)horizontalReference, AnchorRelativeFrom.Line, 6350, 12700));

		var position = AnchorPositionResolver.ResolveAbsolutePosition(anchor, new SectionInfo(), paragraphXTwips: 3000f, paragraphYTwips: 6000f, paragraphWidthTwips: 4500f);

		AssertPosition(position, 3010f, 6020f);
	}

	[Fact]
	public void ResolveAbsolutePosition_MarginEdgeReferences_ResolveAgainstMarginBands()
	{
		var anchor = Anchor(
			457200,
			Placement(AnchorRelativeFrom.RightMargin, AnchorRelativeFrom.BottomMargin, alignX: AnchorAlignment.Right, alignY: AnchorAlignment.Bottom));

		var position = AnchorPositionResolver.ResolveAbsolutePosition(anchor, OneInchMargins(), paragraphXTwips: 0f, paragraphYTwips: 0f, paragraphWidthTwips: 0f);

		// Bands start at 10800 / 14400 twips and are 1440 twips; image is 720 twips.
		AssertPosition(position, 11520f, 15120f);
	}

	[Fact]
	public void ResolveAbsolutePosition_ShapedAnchorPlacementInfo_UsesSharedPlacementModel()
	{
		var anchorPlacement = Placement(
			AnchorRelativeFrom.Margin,
			AnchorRelativeFrom.Margin,
			-6350,
			12700,
			AnchorAlignment.Right,
			AnchorAlignment.Bottom);

		var position = AnchorPositionResolver.ResolveAbsolutePosition(anchorPlacement, 914400, 457200, OneInchMargins(), paragraphXTwips: 0f, paragraphYTwips: 0f, paragraphWidthTwips: 0f);

		AssertPosition(position, 9350f, 13700f);
	}

	[Fact]
	public void ResolveAbsolutePosition_NullAnchor_ThrowsArgumentNullException()
	{
		var act = () => AnchorPositionResolver.ResolveAbsolutePosition(null!, new SectionInfo(), 0f, 0f, 0f);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void ResolveAbsolutePosition_NullSection_ThrowsArgumentNullException()
	{
		var anchor = Anchor(914400, AnchorPlacementInfo.None);

		var act = () => AnchorPositionResolver.ResolveAbsolutePosition(anchor, null!, 0f, 0f, 0f);

		act.Should().Throw<ArgumentNullException>();
	}
}
