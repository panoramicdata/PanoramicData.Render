namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using SkiaSharp;
using Xunit;

public sealed class TextBoxPositioningEngineTests
{
	private readonly MeasurementEngine _engine = new();

	private static SKTypeface GetTypeface()
	{
		var typeface = SKTypeface.FromFamilyName("Arial");
		if (typeface is null || typeface.FamilyName != "Arial")
		{
			Assert.Skip("Arial not available on this platform");
		}

		return typeface;
	}

	private static IReadOnlyList<ParsedRun> MakeLongRun(string text) =>
		[new ParsedRun { Elements = [new TextRunElement { Text = text }] }];

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

	[Fact]
	public void Position_WithInternalMargins_OffsetsInnerContentBox()
	{
		var textFrame = new ShapeTextFrameInfo
		{
			HasTextFrame = true,
			Text = "Hello world",
			LeftInsetEmu = 19050,
			TopInsetEmu = 12700,
			RightInsetEmu = 25400,
			BottomInsetEmu = 6350
		};
		var placement = new AnchorPlacementInfo
		{
			HorizontalRelativeFrom = AnchorRelativeFrom.Page,
			VerticalRelativeFrom = AnchorRelativeFrom.Page,
			HorizontalOffsetEmu = 6350,
			VerticalOffsetEmu = 12700
		};
		var section = new SectionInfo
		{
			PageWidth = 12240,
			PageHeight = 15840
		};

		var positioned = TextBoxPositioningEngine.Position(
			textFrame,
			placement,
			widthEmu: 914400,
			heightEmu: 457200,
			section,
			paragraphXTwips: 0f,
			paragraphYTwips: 0f,
			paragraphWidthTwips: 0f,
			fontFamily: "Arial");

		positioned.XTwips.Should().BeApproximately(10f, 0.001f);
		positioned.YTwips.Should().BeApproximately(20f, 0.001f);
		positioned.ContentXTwips.Should().BeApproximately(40f, 0.001f);
		positioned.ContentYTwips.Should().BeApproximately(40f, 0.001f);
		positioned.ContentWidthTwips.Should().BeApproximately(1370f, 0.001f);
		positioned.ContentBoxHeightTwips.Should().BeApproximately(690f, 0.001f);
	}

	[Fact]
	public void Position_WithShapeAutoFit_ExpandsHeightToFitContent()
	{
		var textFrame = new ShapeTextFrameInfo
		{
			HasTextFrame = true,
			Text = "one two three four five six seven eight nine ten eleven twelve thirteen fourteen fifteen sixteen seventeen eighteen",
			AutoFitMode = ShapeTextAutoFitMode.ShapeAutoFit,
			TopInsetEmu = 12700,
			BottomInsetEmu = 12700
		};
		var placement = new AnchorPlacementInfo
		{
			HorizontalRelativeFrom = AnchorRelativeFrom.Page,
			VerticalRelativeFrom = AnchorRelativeFrom.Page
		};
		var section = new SectionInfo
		{
			PageWidth = 12240,
			PageHeight = 15840
		};

		var positioned = TextBoxPositioningEngine.Position(
			textFrame,
			placement,
			widthEmu: 304800,
			heightEmu: 304800,
			section,
			paragraphXTwips: 0f,
			paragraphYTwips: 0f,
			paragraphWidthTwips: 0f,
			fontFamily: "Arial");

		positioned.ContentHeightTwips.Should().BeGreaterThan(440f);
		positioned.HeightTwips.Should().Be(positioned.ContentHeightTwips + 40f);
		positioned.ContentBoxHeightTwips.Should().Be(positioned.ContentHeightTwips);
	}

	[Fact]
	public void Position_WithNormalAutoFit_DoesNotExpandHeight()
	{
		var textFrame = new ShapeTextFrameInfo
		{
			HasTextFrame = true,
			Text = "one two three four five six seven eight nine ten eleven twelve thirteen fourteen fifteen sixteen seventeen eighteen",
			AutoFitMode = ShapeTextAutoFitMode.NormalAutoFit,
			TopInsetEmu = 12700,
			BottomInsetEmu = 12700
		};
		var placement = new AnchorPlacementInfo
		{
			HorizontalRelativeFrom = AnchorRelativeFrom.Page,
			VerticalRelativeFrom = AnchorRelativeFrom.Page
		};
		var section = new SectionInfo
		{
			PageWidth = 12240,
			PageHeight = 15840
		};

		var positioned = TextBoxPositioningEngine.Position(
			textFrame,
			placement,
			widthEmu: 304800,
			heightEmu: 304800,
			section,
			paragraphXTwips: 0f,
			paragraphYTwips: 0f,
			paragraphWidthTwips: 0f,
			fontFamily: "Arial");

		positioned.ContentHeightTwips.Should().BeGreaterThan(440f);
		positioned.HeightTwips.Should().BeApproximately(480f, 0.001f);
		positioned.ContentBoxHeightTwips.Should().BeApproximately(440f, 0.001f);
	}

	[Fact]
	public void RegisterWrapRegion_SquareWrap_NarrowsParagraphLines()
	{
		var positioned = new PositionedTextBoxLayout(
			XTwips: 2000f,
			YTwips: 0f,
			WidthTwips: 3000f,
			HeightTwips: 480f,
			ContentXTwips: 2000f,
			ContentYTwips: 0f,
			ContentWidthTwips: 3000f,
			ContentBoxHeightTwips: 480f,
			Blocks: [],
			ContentHeightTwips: 0f,
			AnchorPlacement: new AnchorPlacementInfo { WrapStyle = AnchorWrapStyle.Square });
		var registry = new WrapRegionRegistry();
		TextBoxPositioningEngine.RegisterWrapRegion(positioned, registry);

		var breaker = new ParagraphLineBreaker(_engine);
		var typeface = GetTypeface();
		var runs = MakeLongRun("The quick brown fox jumps over the lazy dog and keeps running forward");
		var linesWithout = breaker.ComputeLineBreaks(runs, typeface, 12f, 5000f);
		var linesWith = breaker.ComputeLineBreaks(runs, typeface, 12f, 5000f, registry, 0f, 0f, 240f);

		linesWith.Count.Should().BeGreaterThan(linesWithout.Count);
	}

	[Fact]
	public void RegisterWrapRegion_ColumnRelativeSquareWrap_AffectsOnlyAnchorColumn()
	{
		var section = new SectionInfo { ColumnCount = 2 };
		var columns = PageBuilder.ComputeColumnRegions(section);
		var leftColumn = columns[0];
		var rightColumn = columns[1];
		var textFrame = new ShapeTextFrameInfo
		{
			HasTextFrame = true,
			Text = "Column note"
		};
		var placement = new AnchorPlacementInfo
		{
			HorizontalRelativeFrom = AnchorRelativeFrom.Column,
			VerticalRelativeFrom = AnchorRelativeFrom.Paragraph,
			HorizontalAlignment = AnchorAlignment.Right,
			WrapStyle = AnchorWrapStyle.Square
		};

		var positioned = TextBoxPositioningEngine.Position(
			textFrame,
			placement,
			widthEmu: 914400,
			heightEmu: 457200,
			section,
			paragraphXTwips: leftColumn.XTwips,
			paragraphYTwips: 0f,
			paragraphWidthTwips: leftColumn.WidthTwips,
			fontFamily: "Arial");

		positioned.XTwips.Should().BeApproximately(leftColumn.XTwips + leftColumn.WidthTwips - 1440f, 0.001f);

		var registry = new WrapRegionRegistry();
		TextBoxPositioningEngine.RegisterWrapRegion(positioned, registry);

		var breaker = new ParagraphLineBreaker(_engine);
		var typeface = GetTypeface();
		var runs = MakeLongRun("The quick brown fox jumps over the lazy dog and keeps running forward");

		var leftLinesWithout = breaker.ComputeLineBreaks(runs, typeface, 12f, leftColumn.WidthTwips);
		var leftLinesWith = breaker.ComputeLineBreaks(runs, typeface, 12f, leftColumn.WidthTwips, registry, leftColumn.XTwips, 0f, 240f);
		var rightLinesWithout = breaker.ComputeLineBreaks(runs, typeface, 12f, rightColumn.WidthTwips);
		var rightLinesWith = breaker.ComputeLineBreaks(runs, typeface, 12f, rightColumn.WidthTwips, registry, rightColumn.XTwips, 0f, 240f);

		leftLinesWith.Count.Should().BeGreaterThan(leftLinesWithout.Count);
		rightLinesWith.Count.Should().Be(rightLinesWithout.Count);
	}

	[Fact]
	public void RegisterWrapRegion_TopBottomWrap_BlocksAffectedLines()
	{
		var positioned = new PositionedTextBoxLayout(
			XTwips: 1000f,
			YTwips: 0f,
			WidthTwips: 2000f,
			HeightTwips: 240f,
			ContentXTwips: 1000f,
			ContentYTwips: 0f,
			ContentWidthTwips: 2000f,
			ContentBoxHeightTwips: 240f,
			Blocks: [],
			ContentHeightTwips: 0f,
			AnchorPlacement: new AnchorPlacementInfo { WrapStyle = AnchorWrapStyle.TopAndBottom });
		var registry = new WrapRegionRegistry();
		TextBoxPositioningEngine.RegisterWrapRegion(positioned, registry);

		registry.GetAvailableSegments(0f, 5000f, 0f, 240f).Should().BeEmpty();
	}

	[Fact]
	public void RegisterWrapRegion_TightWrap_RegistersRectangularPolygonExclusion()
	{
		var positioned = new PositionedTextBoxLayout(
			XTwips: 2500f,
			YTwips: 0f,
			WidthTwips: 1500f,
			HeightTwips: 240f,
			ContentXTwips: 2500f,
			ContentYTwips: 0f,
			ContentWidthTwips: 1500f,
			ContentBoxHeightTwips: 240f,
			Blocks: [],
			ContentHeightTwips: 0f,
			AnchorPlacement: new AnchorPlacementInfo { WrapStyle = AnchorWrapStyle.Tight });
		var registry = new WrapRegionRegistry();
		TextBoxPositioningEngine.RegisterWrapRegion(positioned, registry);

		var segments = registry.GetAvailableSegments(0f, 5000f, 0f, 240f);
		segments.Should().HaveCount(2);
		segments[0].XTwips.Should().Be(0f);
		segments[0].WidthTwips.Should().Be(2500f);
		segments[1].XTwips.Should().Be(4000f);
		segments[1].WidthTwips.Should().Be(1000f);
	}

	[Fact]
	public void RegisterWrapRegion_BehindDocument_DoesNotDisplaceText()
	{
		var positioned = new PositionedTextBoxLayout(
			XTwips: 2000f,
			YTwips: 0f,
			WidthTwips: 3000f,
			HeightTwips: 480f,
			ContentXTwips: 2000f,
			ContentYTwips: 0f,
			ContentWidthTwips: 3000f,
			ContentBoxHeightTwips: 480f,
			Blocks: [],
			ContentHeightTwips: 0f,
			AnchorPlacement: new AnchorPlacementInfo { WrapStyle = AnchorWrapStyle.Square, BehindDocument = true });
		var registry = new WrapRegionRegistry();
		TextBoxPositioningEngine.RegisterWrapRegion(positioned, registry);

		registry.IsEmpty.Should().BeTrue();
	}
}