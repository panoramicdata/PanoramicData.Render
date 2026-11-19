namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using RenderTabStop = PanoramicData.Render.TabStop;

public sealed partial class RenderCommandEmitterTests
{
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
		=> new() { SourceElement = new Paragraph(new Run(new Text(text))) };

	private static LayoutBlock NumberedBlock(string text, int numberingId, int numberingLevel)
		=> new(new ParagraphBlock { SourceElement = new Paragraph(new Run(new Text(text))), NumberingId = numberingId, NumberingLevel = numberingLevel }, 300f);

	private static LayoutPage WatermarkPage(WatermarkInfo watermark)
		=> TestLayoutPageFactory.Create([new LayoutBlock(ParagraphBlockOf("Body text"), 300f)], 1, watermark: watermark);
}