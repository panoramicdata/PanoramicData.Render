namespace PanoramicData.Render.Test;

using A = DocumentFormat.OpenXml.Drawing;
using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using RenderTabStop = PanoramicData.Render.TabStop;
using static PanoramicData.Render.Test.EmitterBuilders;
using FakeRenderTarget = PanoramicData.Render.Test.RenderCommandEmitterTests.FakeRenderTarget;

internal static class EmitterBuilders2
{
	internal const string Tab = "\t";

	internal static Paragraph TabbedParagraph(TabStopValues alignment, int position, params string[] parts)
	{
		var paragraph = new Paragraph(new ParagraphProperties(new Tabs(new DocumentFormat.OpenXml.Wordprocessing.TabStop { Val = alignment, Position = position })));
		foreach (var part in parts)
		{
			paragraph.AppendChild(part == Tab ? new Run(new TabChar()) : new Run(new Text(part) { Space = SpaceProcessingModeValues.Preserve }));
		}

		return paragraph;
	}

	internal static float ExpectedTabX(TabStopType type, float position, string beforeDecimal)
		=> 720f + TabStopResolver.ComputeContentStart(new RenderTabStop(position, type), 0f, EstimateWidth(beforeDecimal, 12f));

	internal const int ContentWidthTwips = 12240 - 720 - 720;

	internal static float ExpectedRightTabX(string text)
		=> 720f + TabStopResolver.ComputeContentStart(new RenderTabStop(9360f, TabStopType.Right), EstimateWidth(text, 12f));

	internal static void AssertSingleText(FakeRenderTarget target, string text)
	{
		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].Text.Should().Be(text);
	}

	internal static FakeRenderTarget EmitAlignedParagraph(Paragraph paragraph, bool isBiDi = false, ParagraphAlignment? alignment = null)
	{
		var block = new ParagraphBlock { SourceElement = paragraph, IsBiDi = isBiDi, Alignment = alignment };
		var section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 };
		return Emit(TestLayoutPageFactory.Create([new LayoutBlock(block, 300f)], 1, section: section));
	}

	internal static LayoutPage BandPage(int pageNumber, Paragraph paragraph, bool header)
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

	internal static Paragraph SimpleFieldParagraph(string cachedText, string instruction)
		=> new(new SimpleField(new Run(new Text(cachedText))) { Instruction = instruction });

	internal static Paragraph ComplexFieldParagraph(string code, string cachedText)
		=> new(
			new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
			new Run(new FieldCode(code)),
			new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }),
			new Run(new Text(cachedText)),
			new Run(new FieldChar { FieldCharType = FieldCharValues.End }));

	internal static ParagraphBlock ParagraphBlockOf(string text)
		=> ParagraphBlockOf(new Paragraph(new Run(new Text(text))));

	internal static ParagraphBlock ParagraphBlockOf(Paragraph paragraph)
		=> new() { SourceElement = paragraph };

	internal static FakeRenderTarget EmitBlock(ParagraphBlock block)
		=> Emit(TestLayoutPageFactory.Create([new LayoutBlock(block, 300f)], 1, section: NarrowSection()));

	internal static LayoutBlock NumberedBlock(string text, int numberingId, int numberingLevel)
		=> new(new ParagraphBlock { SourceElement = new Paragraph(new Run(new Text(text))), NumberingId = numberingId, NumberingLevel = numberingLevel }, 300f);

	internal static LayoutPage WatermarkPage(WatermarkInfo watermark)
		=> TestLayoutPageFactory.Create([new LayoutBlock(ParagraphBlockOf("Body text"), 300f)], 1, watermark: watermark);

	internal static float EstimateWidth(string text, float sizePoints)
	{
		// Must match RenderCommandEmitter.EstimateTextWidthTwips: text.Length * sizePoints * AverageGlyphWidthFactor (10)
		return text.Length * sizePoints * 10f;
	}

	internal static Drawing CreateInlineDrawing(string relationshipId, int widthTwips, int heightTwips)
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
