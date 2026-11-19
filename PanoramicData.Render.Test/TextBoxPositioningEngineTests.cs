namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using SkiaSharp;
using Xunit;

public sealed class TextBoxPositioningEngineTests
{
	// Resolves "Arial" to the shipped Liberation Sans, so layout does not depend on the machine's fonts.
	private static readonly FontResolver FontResolver = TestFonts.CreateResolver();

	private const string LongSentence = "The quick brown fox jumps over the lazy dog and keeps running forward";

	private readonly MeasurementEngine _engine = new();

	// The shipped test font, not a system font, so the results are the same on every machine.
	private static SKTypeface GetTypeface() => TestFonts.Sans;

	private static IReadOnlyList<ParsedRun> MakeLongRun(string text) =>
		[new ParsedRun { Elements = [new TextRunElement { Text = text }] }];

	private static SectionInfo PageOnly() => new() { PageWidth = 12240, PageHeight = 15840 };

	private static PositionedTextBoxLayout Position(
		ShapeTextFrameInfo textFrame,
		AnchorPlacementInfo placement,
		SectionInfo section,
		long widthEmu = 914400,
		long heightEmu = 457200,
		ParagraphBox? paragraph = null)
		=> TextBoxPositioningEngine.Position(
			textFrame,
			placement,
			widthEmu,
			heightEmu,
			section,
			paragraph: paragraph ?? new ParagraphBox(0f, 0f, 0f),
			font: new TextBoxFont("Arial", FontResolver: FontResolver));

	private static AnchorPlacementInfo PagePlacement() => new()
	{
		HorizontalRelativeFrom = AnchorRelativeFrom.Page,
		VerticalRelativeFrom = AnchorRelativeFrom.Page
	};

	private static PositionedTextBoxLayout Layout(float x, float width, float height, AnchorPlacementInfo placement)
		=> new(
			XTwips: x,
			YTwips: 0f,
			WidthTwips: width,
			HeightTwips: height,
			ContentXTwips: x,
			ContentYTwips: 0f,
			ContentWidthTwips: width,
			ContentBoxHeightTwips: height,
			Blocks: [],
			ContentHeightTwips: 0f,
			AnchorPlacement: placement);

	private static WrapRegionRegistry RegistryFor(PositionedTextBoxLayout layout)
	{
		var registry = new WrapRegionRegistry();
		TextBoxPositioningEngine.RegisterWrapRegion(layout, registry);
		return registry;
	}

	private static PositionedTextBoxLayout PositionAutoFit(ShapeTextAutoFitMode mode)
		=> Position(
			new ShapeTextFrameInfo
			{
				HasTextFrame = true,
				Text = "one two three four five six seven eight nine ten eleven twelve thirteen fourteen fifteen sixteen seventeen eighteen",
				AutoFitMode = mode,
				TopInsetEmu = 12700,
				BottomInsetEmu = 12700
			},
			PagePlacement(),
			PageOnly(),
			widthEmu: 304800,
			heightEmu: 304800);

	[Fact]
	public void Position_NullTextFrame_ThrowsArgumentNullException()
	{
		var act = () => TextBoxPositioningEngine.Position(null!, new AnchorPlacementInfo(), 914400, 457200, new SectionInfo(), new ParagraphBox(0f, 0f, 0f));

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("textFrame");
	}

	[Fact]
	public void Position_NonPositiveWidth_ThrowsArgumentOutOfRangeException()
	{
		var textFrame = new ShapeTextFrameInfo { HasTextFrame = true, Text = "Hello" };

		var act = () => TextBoxPositioningEngine.Position(textFrame, new AnchorPlacementInfo(), 0, 457200, new SectionInfo(), new ParagraphBox(0f, 0f, 0f));

		act.Should().Throw<ArgumentOutOfRangeException>()
			.WithParameterName("widthEmu");
	}

	[Fact]
	public void Position_ComputesAbsoluteLocationAndLayoutsContent()
	{
		var section = new SectionInfo
		{
			PageWidth = 12240,
			PageHeight = 15840,
			MarginLeft = 1440,
			MarginRight = 1440,
			MarginTop = 1440,
			MarginBottom = 1440
		};

		var positioned = Position(
			new ShapeTextFrameInfo { HasTextFrame = true, Text = "A long enough sentence to wrap inside the text box" },
			new AnchorPlacementInfo
			{
				HorizontalRelativeFrom = AnchorRelativeFrom.Margin,
				VerticalRelativeFrom = AnchorRelativeFrom.Paragraph,
				HorizontalAlignment = AnchorAlignment.Center,
				VerticalOffsetEmu = 12700,
				WrapStyle = AnchorWrapStyle.Square
			},
			section,
			paragraph: new ParagraphBox(2500f, 3000f, 4000f));

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
		var positioned = Position(
			new ShapeTextFrameInfo
			{
				HasTextFrame = true,
				Text = "Hello world",
				LeftInsetEmu = 19050,
				TopInsetEmu = 12700,
				RightInsetEmu = 25400,
				BottomInsetEmu = 6350
			},
			new AnchorPlacementInfo
			{
				HorizontalRelativeFrom = AnchorRelativeFrom.Page,
				VerticalRelativeFrom = AnchorRelativeFrom.Page,
				HorizontalOffsetEmu = 6350,
				VerticalOffsetEmu = 12700
			},
			PageOnly());

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
		var positioned = PositionAutoFit(ShapeTextAutoFitMode.ShapeAutoFit);

		positioned.ContentHeightTwips.Should().BeGreaterThan(440f);
		positioned.HeightTwips.Should().Be(positioned.ContentHeightTwips + 40f);
		positioned.ContentBoxHeightTwips.Should().Be(positioned.ContentHeightTwips);
	}

	[Fact]
	public void Position_WithNormalAutoFit_DoesNotExpandHeight()
	{
		var positioned = PositionAutoFit(ShapeTextAutoFitMode.NormalAutoFit);

		positioned.ContentHeightTwips.Should().BeGreaterThan(440f);
		positioned.HeightTwips.Should().BeApproximately(480f, 0.001f);
		positioned.ContentBoxHeightTwips.Should().BeApproximately(440f, 0.001f);
	}

	[Fact]
	public void RegisterWrapRegion_SquareWrap_NarrowsParagraphLines()
	{
		var registry = RegistryFor(Layout(2000f, 3000f, 480f, new AnchorPlacementInfo { WrapStyle = AnchorWrapStyle.Square }));

		var breaker = new ParagraphLineBreaker(_engine);
		var typeface = GetTypeface();
		var runs = MakeLongRun(LongSentence);
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

		var positioned = Position(
			new ShapeTextFrameInfo { HasTextFrame = true, Text = "Column note" },
			new AnchorPlacementInfo
			{
				HorizontalRelativeFrom = AnchorRelativeFrom.Column,
				VerticalRelativeFrom = AnchorRelativeFrom.Paragraph,
				HorizontalAlignment = AnchorAlignment.Right,
				WrapStyle = AnchorWrapStyle.Square
			},
			section,
			paragraph: new ParagraphBox(leftColumn.XTwips, 0f, leftColumn.WidthTwips));

		positioned.XTwips.Should().BeApproximately(leftColumn.XTwips + leftColumn.WidthTwips - 1440f, 0.001f);

		var registry = RegistryFor(positioned);
		var breaker = new ParagraphLineBreaker(_engine);
		var typeface = GetTypeface();
		var runs = MakeLongRun(LongSentence);

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
		var registry = RegistryFor(Layout(1000f, 2000f, 240f, new AnchorPlacementInfo { WrapStyle = AnchorWrapStyle.TopAndBottom }));

		registry.GetAvailableSegments(0f, 5000f, 0f, 240f).Should().BeEmpty();
	}

	[Fact]
	public void RegisterWrapRegion_TightWrap_RegistersRectangularPolygonExclusion()
	{
		var registry = RegistryFor(Layout(2500f, 1500f, 240f, new AnchorPlacementInfo { WrapStyle = AnchorWrapStyle.Tight }));

		WrapAssert.Segments(registry.GetAvailableSegments(0f, 5000f, 0f, 240f), (0f, 2500f), (4000f, 1000f));
	}

	[Fact]
	public void RegisterWrapRegion_BehindDocument_DoesNotDisplaceText()
	{
		var registry = RegistryFor(Layout(2000f, 3000f, 480f, new AnchorPlacementInfo { WrapStyle = AnchorWrapStyle.Square, BehindDocument = true }));

		registry.IsEmpty.Should().BeTrue();
	}
}
