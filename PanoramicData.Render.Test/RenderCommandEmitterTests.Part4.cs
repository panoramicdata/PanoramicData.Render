namespace PanoramicData.Render.Test;

using A = DocumentFormat.OpenXml.Drawing;
using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using Xunit;
using RenderTabStop = PanoramicData.Render.TabStop;

public sealed partial class RenderCommandEmitterTests
{
	[Theory]
	[InlineData("hyphen", true, 9360, "-")]
	[InlineData("underscore", false, 4320, "_")]
	[InlineData("middledot", false, 4320, "\u00B7")]
	public void EmitPage_Leader_DrawsLeaderCharactersBeforeTabStop(string leader, bool right, int position, string expected)
	{
		var paragraph = TabbedParagraph(right ? TabStopValues.Right : TabStopValues.Left, position, Tab, "End");
		paragraph.Descendants<DocumentFormat.OpenXml.Wordprocessing.TabStop>().Single().Leader = leader switch
		{
			"hyphen" => TabStopLeaderCharValues.Hyphen,
			"underscore" => TabStopLeaderCharValues.Underscore,
			_ => TabStopLeaderCharValues.MiddleDot
		};
		var target = EmitParagraph(paragraph);

		target.DrawTextCalls.Where(c => c.Text == expected).Should().NotBeEmpty();
	}

	[Fact]
	public void EmitPage_NoLeader_DoesNotDrawLeaderCharacters()
	{
		var target = EmitParagraph(TabbedParagraph(TabStopValues.Left, 4320, "A", Tab, "B"));

		target.DrawTextCalls.Should().HaveCount(2);
		target.DrawTextCalls[0].Text.Should().Be("A");
		target.DrawTextCalls[1].Text.Should().Be("B");
	}

	[Fact]
	public void EmitPage_HeaderWithRightTab_DrawsHeaderTextAligned()
	{
		var target = Emit(BandPage(1, TabbedParagraph(TabStopValues.Right, 9360, "Title", Tab, "Page 1"), header: true));

		target.DrawTextCalls.Should().HaveCount(2);
		target.DrawTextCalls[0].Text.Should().Be("Title");
		target.DrawTextCalls[1].Text.Should().Be("Page 1");
		target.DrawTextCalls[1].BaselineXTwips.Should().Be(ExpectedRightTabX("Page 1"));
	}

	[Fact]
	public void EmitPage_FooterWithRightTab_DrawsFooterTextAligned()
	{
		var target = Emit(BandPage(2, TabbedParagraph(TabStopValues.Right, 9360, Tab, "Page 2"), header: false));

		AssertSingleText(target, "Page 2");
		target.DrawTextCalls[0].BaselineXTwips.Should().Be(ExpectedRightTabX("Page 2"));
	}

	[Fact]
	public void EmitPage_HeaderInlineImageTallerThanLine_PushesTextBaselineDown()
	{
		var headerParagraph = new Paragraph(
			new Run(new Text("Title") { Space = SpaceProcessingModeValues.Preserve }),
			new Run(new TabChar()),
			new Run(CreateInlineDrawing("rId-logo", widthTwips: 720, heightTwips: 900)));
		var images = new Dictionary<string, ImageData>
		{
			["rId-logo"] = new ImageData([1, 2, 3], "image/png")
		};

		var target = Emit(BandPage(2, headerParagraph, header: true), images: images);

		target.DrawTextCalls.Should().NotBeEmpty();
		target.DrawTextCalls[0].Text.Should().Be("Title");
		target.DrawTextCalls[0].BaselineYTwips.Should().BeGreaterThan(960f);
	}

	[Fact]
	public void EmitPage_RtlRunInParagraph_DrawsText()
	{
		var paragraph = new Paragraph(
			new Run(
				new RunProperties(new RightToLeftText()),
				new Text("مرحبا")));
		var target = EmitParagraph(paragraph);

		AssertSingleText(target, "مرحبا");
	}

	[Fact]
	public void EmitPage_BiDiParagraphNoAlignment_DefaultsToRightAligned()
	{
		var paragraph = new Paragraph(
			new ParagraphProperties(new BiDi()),
			new Run(new Text("Test")));
		var target = EmitAlignedParagraph(paragraph, isBiDi: true);

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].BaselineXTwips.Should().Be(720f + ContentWidthTwips - EstimateWidth("Test", 12f));
	}

	[Fact]
	public void EmitPage_BiDiParagraphExplicitLeftAlignment_LeftAligned()
	{
		var paragraph = new Paragraph(
			new ParagraphProperties(new BiDi(), new Justification { Val = JustificationValues.Left }),
			new Run(new Text("Test")));
		var target = EmitAlignedParagraph(paragraph, isBiDi: true, alignment: ParagraphAlignment.Left);

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].BaselineXTwips.Should().Be(720f);
	}

	[Fact]
	public void EmitPage_CenterAlignedParagraph_TextCentered()
	{
		var paragraph = new Paragraph(
			new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
			new Run(new Text("Hi")));
		var target = EmitAlignedParagraph(paragraph, alignment: ParagraphAlignment.Center);

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].BaselineXTwips.Should().Be(720f + (ContentWidthTwips - EstimateWidth("Hi", 12f)) / 2f);
	}

	[Fact]
	public void EmitPage_InlineSdtRun_RendersInnerContent()
	{
		// An inline content control (SdtRun) wrapping a run should render the inner text
		var sdt = new SdtRun(
			new SdtProperties(),
			new SdtContentRun(
				new Run(new Text("Controlled text"))));
		var paragraph = new Paragraph(sdt);
		var target = EmitParagraph(paragraph);

		AssertSingleText(target, "Controlled text");
	}

	[Fact]
	public void EmitPage_SdtRunMixedWithNormalRun_RendersBoth()
	{
		var sdt = new SdtRun(
			new SdtProperties(),
			new SdtContentRun(
				new Run(new Text("SDT") { Space = SpaceProcessingModeValues.Preserve })));
		var paragraph = new Paragraph(
			new Run(new Text("Normal ") { Space = SpaceProcessingModeValues.Preserve }),
			sdt);
		var target = EmitParagraph(paragraph);

		// Both text segments should be rendered (may merge into one if same font)
		var allText = string.Concat(target.DrawTextCalls.Select(c => c.Text));
		allText.Should().Contain("Normal ");
		allText.Should().Contain("SDT");
	}

	private static SectionInfo NarrowSection() => new() { MarginLeft = 500, MarginRight = 500, PageWidth = 10000 };

	private static FakeRenderTarget Emit(
		LayoutPage page,
		RenderOptions? options = null,
		int? totalPageCount = null,
		DateTime? renderTimestampUtc = null,
		IReadOnlyDictionary<string, ImageData>? images = null)
	{
		var target = new FakeRenderTarget();
		RenderCommandEmitter.EmitPage(page, target, options, totalPageCount, renderTimestampUtc, images: images);
		return target;
	}

	private static FakeRenderTarget EmitParagraph(
		Paragraph paragraph,
		SectionInfo? section = null,
		int pageNumber = 1,
		RenderOptions? options = null,
		int? totalPageCount = null,
		DateTime? renderTimestampUtc = null,
		IReadOnlyDictionary<string, ImageData>? images = null)
	{
		var page = TestLayoutPageFactory.Create([new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)], pageNumber, section: section);
		return Emit(page, options, totalPageCount, renderTimestampUtc, images);
	}

	private const string Tab = "\t";

	private static Paragraph TabbedParagraph(TabStopValues alignment, int position, params string[] parts)
	{
		var paragraph = new Paragraph(new ParagraphProperties(new Tabs(new DocumentFormat.OpenXml.Wordprocessing.TabStop { Val = alignment, Position = position })));
		foreach (var part in parts)
		{
			paragraph.AppendChild(part == Tab ? new Run(new TabChar()) : new Run(new Text(part) { Space = SpaceProcessingModeValues.Preserve }));
		}

		return paragraph;
	}

	private static float ExpectedTabX(TabStopType type, float position, string beforeDecimal)
		=> 720f + TabStopResolver.ComputeContentStart(new RenderTabStop(position, type), 0f, EstimateWidth(beforeDecimal, 12f));

	private const int ContentWidthTwips = 12240 - 720 - 720;

	private static float ExpectedRightTabX(string text)
		=> 720f + TabStopResolver.ComputeContentStart(new RenderTabStop(9360f, TabStopType.Right), EstimateWidth(text, 12f));

	private static void AssertSingleText(FakeRenderTarget target, string text)
	{
		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].Text.Should().Be(text);
	}

	private static FakeRenderTarget EmitAlignedParagraph(Paragraph paragraph, bool isBiDi = false, ParagraphAlignment? alignment = null)
	{
		var block = new ParagraphBlock { SourceElement = paragraph, IsBiDi = isBiDi, Alignment = alignment };
		var section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 };
		return Emit(TestLayoutPageFactory.Create([new LayoutBlock(block, 300f)], 1, section: section));
	}

	private static LayoutPage BandPage(int pageNumber, Paragraph paragraph, bool header)
	{
		IReadOnlyList<LayoutBlock> band = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 240f)];
		return new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = pageNumber,
			ContentTopTwips = 1440,
			HeaderTopTwips = header ? 720 : 0,
			FooterTopTwips = header ? 0 : 14400,
			HeaderBlocks = header ? band : null,
			FooterBlocks = header ? null : band,
			Blocks = []
		};
	}

	private static Paragraph SimpleFieldParagraph(string cachedText, string instruction)
		=> new(new SimpleField(new Run(new Text(cachedText))) { Instruction = instruction });

	private static Paragraph ComplexFieldParagraph(string code, string cachedText)
		=> new(
			new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
			new Run(new FieldCode(code)),
			new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }),
			new Run(new Text(cachedText)),
			new Run(new FieldChar { FieldCharType = FieldCharValues.End }));

	private static ParagraphBlock ParagraphBlockOf(string text)
		=> ParagraphBlockOf(new Paragraph(new Run(new Text(text))));

	private static ParagraphBlock ParagraphBlockOf(Paragraph paragraph)
		=> new() { SourceElement = paragraph };

	private static FakeRenderTarget EmitBlock(ParagraphBlock block)
		=> Emit(TestLayoutPageFactory.Create([new LayoutBlock(block, 300f)], 1, section: NarrowSection()));

	private static LayoutBlock NumberedBlock(string text, int numberingId, int numberingLevel)
		=> new(new ParagraphBlock { SourceElement = new Paragraph(new Run(new Text(text))), NumberingId = numberingId, NumberingLevel = numberingLevel }, 300f);

	private static LayoutPage WatermarkPage(WatermarkInfo watermark)
		=> TestLayoutPageFactory.Create([new LayoutBlock(ParagraphBlockOf("Body text"), 300f)], 1, watermark: watermark);

	private static float EstimateWidth(string text, float sizePoints)
	{
		// Must match RenderCommandEmitter.EstimateTextWidthTwips: text.Length * sizePoints * AverageGlyphWidthFactor (10)
		return text.Length * sizePoints * 10f;
	}

	private static Drawing CreateInlineDrawing(string relationshipId, int widthTwips, int heightTwips)
	{
		var blip = new A.Blip { Embed = relationshipId };
		var graphicData = new A.GraphicData(blip)
		{
			Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture"
		};
		var graphic = new A.Graphic(graphicData);
		const float emusPerTwip = 635f;
		var inline = new DW.Inline(
			new DW.Extent
			{
				Cx = (long)(widthTwips * emusPerTwip),
				Cy = (long)(heightTwips * emusPerTwip)
			},
			graphic);
		return new Drawing(inline);
	}
}
