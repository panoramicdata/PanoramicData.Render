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
	[Fact]
	public void EmitPage_HyphenLeader_DrawsHyphensBeforeTabStop()
	{
		var paragraph = new Paragraph(
			new ParagraphProperties(
				new Tabs(
					new DocumentFormat.OpenXml.Wordprocessing.TabStop
					{
						Val = TabStopValues.Right,
						Position = 9360,
						Leader = TabStopLeaderCharValues.Hyphen
					}
				)),
			new Run(new TabChar()),
			new Run(new Text("End") { Space = SpaceProcessingModeValues.Preserve }));
		var page = TestLayoutPageFactory.Create([new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)], 1);
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Where(c => c.Text == "-").Should().NotBeEmpty();
	}

	[Fact]
	public void EmitPage_UnderscoreLeader_DrawsUnderscoresBeforeTabStop()
	{
		var paragraph = new Paragraph(
			new ParagraphProperties(
				new Tabs(
					new DocumentFormat.OpenXml.Wordprocessing.TabStop
					{
						Val = TabStopValues.Left,
						Position = 4320,
						Leader = TabStopLeaderCharValues.Underscore
					}
				)),
			new Run(new TabChar()),
			new Run(new Text("Value") { Space = SpaceProcessingModeValues.Preserve }));
		var page = TestLayoutPageFactory.Create([new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)], 1);
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Where(c => c.Text == "_").Should().NotBeEmpty();
	}

	[Fact]
	public void EmitPage_MiddleDotLeader_DrawsMiddleDotsBeforeTabStop()
	{
		var paragraph = new Paragraph(
			new ParagraphProperties(
				new Tabs(
					new DocumentFormat.OpenXml.Wordprocessing.TabStop
					{
						Val = TabStopValues.Left,
						Position = 4320,
						Leader = TabStopLeaderCharValues.MiddleDot
					}
				)),
			new Run(new TabChar()),
			new Run(new Text("Price") { Space = SpaceProcessingModeValues.Preserve }));
		var page = TestLayoutPageFactory.Create([new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)], 1);
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Where(c => c.Text == "\u00B7").Should().NotBeEmpty();
	}

	[Fact]
	public void EmitPage_NoLeader_DoesNotDrawLeaderCharacters()
	{
		var paragraph = new Paragraph(
			new ParagraphProperties(
				new Tabs(
					new DocumentFormat.OpenXml.Wordprocessing.TabStop
					{
						Val = TabStopValues.Left,
						Position = 4320
						// No Leader specified — defaults to None
					}
				)),
			new Run(new Text("A") { Space = SpaceProcessingModeValues.Preserve }),
			new Run(new TabChar()),
			new Run(new Text("B") { Space = SpaceProcessingModeValues.Preserve }));
		var page = TestLayoutPageFactory.Create([new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)], 1);
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		// Only "A" and "B" should be drawn — no leader characters
		target.DrawTextCalls.Should().HaveCount(2);
		target.DrawTextCalls[0].Text.Should().Be("A");
		target.DrawTextCalls[1].Text.Should().Be("B");
	}

	[Fact]
	public void EmitPage_HeaderWithRightTab_DrawsHeaderTextAligned()
	{
		// Common header pattern: "Title" <tab> "Page 1" right-aligned
		var headerParagraph = new Paragraph(
			new ParagraphProperties(
				new Tabs(
					new DocumentFormat.OpenXml.Wordprocessing.TabStop
					{
						Val = TabStopValues.Right,
						Position = 9360
					}
				)),
			new Run(new Text("Title") { Space = SpaceProcessingModeValues.Preserve }),
			new Run(new TabChar()),
			new Run(new Text("Page 1") { Space = SpaceProcessingModeValues.Preserve }));
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1440,
			HeaderTopTwips = 720,
			HeaderBlocks = [new LayoutBlock(new ParagraphBlock { SourceElement = headerParagraph }, 240f)],
			Blocks = []
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		// Should have "Title" and "Page 1"
		target.DrawTextCalls.Should().HaveCount(2);
		target.DrawTextCalls[0].Text.Should().Be("Title");
		target.DrawTextCalls[1].Text.Should().Be("Page 1");

		// "Page 1" should be right-aligned at tab stop 9360
		var contentWidth = EstimateWidth("Page 1", 12f);
		var expectedX = 720f + TabStopResolver.ComputeContentStart(
			new RenderTabStop(9360f, TabStopType.Right),
			contentWidth);
		target.DrawTextCalls[1].BaselineXTwips.Should().Be(expectedX);
	}

	[Fact]
	public void EmitPage_FooterWithRightTab_DrawsFooterTextAligned()
	{
		var footerParagraph = new Paragraph(
			new ParagraphProperties(
				new Tabs(
					new DocumentFormat.OpenXml.Wordprocessing.TabStop
					{
						Val = TabStopValues.Right,
						Position = 9360
					}
				)),
			new Run(new TabChar()),
			new Run(new Text("Page 2") { Space = SpaceProcessingModeValues.Preserve }));
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 2,
			ContentTopTwips = 1440,
			FooterTopTwips = 14400,
			FooterBlocks = [new LayoutBlock(new ParagraphBlock { SourceElement = footerParagraph }, 240f)],
			Blocks = []
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].Text.Should().Be("Page 2");

		var contentWidth = EstimateWidth("Page 2", 12f);
		var expectedX = 720f + TabStopResolver.ComputeContentStart(
			new RenderTabStop(9360f, TabStopType.Right),
			contentWidth);
		target.DrawTextCalls[0].BaselineXTwips.Should().Be(expectedX);
	}

	[Fact]
	public void EmitPage_HeaderInlineImageTallerThanLine_PushesTextBaselineDown()
	{
		var headerParagraph = new Paragraph(
			new Run(new Text("Title") { Space = SpaceProcessingModeValues.Preserve }),
			new Run(new TabChar()),
			new Run(CreateInlineDrawing("rId-logo", widthTwips: 720, heightTwips: 900)));
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 2,
			ContentTopTwips = 1440,
			HeaderTopTwips = 720,
			HeaderBlocks = [new LayoutBlock(new ParagraphBlock { SourceElement = headerParagraph }, 240f)],
			Blocks = []
		};
		var target = new FakeRenderTarget();
		var images = new Dictionary<string, ImageData>
		{
			["rId-logo"] = new ImageData([1, 2, 3], "image/png")
		};

		RenderCommandEmitter.EmitPage(page, target, images: images);

		target.DrawTextCalls.Should().NotBeEmpty();
		target.DrawTextCalls[0].Text.Should().Be("Title");
		target.DrawTextCalls[0].BaselineYTwips.Should().BeGreaterThan(960f);
	}

	[Fact]
	public void EmitPage_RtlRunInParagraph_DrawsText()
	{
		// An RTL run should still produce DrawText output (detection only for now)
		var paragraph = new Paragraph(
			new Run(
				new RunProperties(new RightToLeftText()),
				new Text("مرحبا")));
		var page = TestLayoutPageFactory.Create([new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)], 1);
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].Text.Should().Be("مرحبا");
	}

	[Fact]
	public void EmitPage_BiDiParagraphNoAlignment_DefaultsToRightAligned()
	{
		// A BiDi paragraph with no explicit alignment should default to right-aligned
		var paragraph = new Paragraph(
			new ParagraphProperties(new BiDi()),
			new Run(new Text("Test")));
		var section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 };
		var contentWidth = section.PageWidth - section.MarginLeft - section.MarginRight;
		var page = TestLayoutPageFactory.Create([new LayoutBlock(new ParagraphBlock { SourceElement = paragraph, IsBiDi = true }, 300f)], 1, section: section);
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().ContainSingle();
		// Text should start at right margin minus text width
		var textWidth = EstimateWidth("Test", 12f);
		var expectedX = section.MarginLeft + contentWidth - textWidth;
		target.DrawTextCalls[0].BaselineXTwips.Should().Be(expectedX);
	}

	[Fact]
	public void EmitPage_BiDiParagraphExplicitLeftAlignment_LeftAligned()
	{
		// A BiDi paragraph with explicit left alignment should be left-aligned
		var paragraph = new Paragraph(
			new ParagraphProperties(new BiDi(), new Justification { Val = JustificationValues.Left }),
			new Run(new Text("Test")));
		var section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 };
		var page = TestLayoutPageFactory.Create([new LayoutBlock(new ParagraphBlock { SourceElement = paragraph, IsBiDi = true, Alignment = ParagraphAlignment.Left }, 300f)], 1, section: section);
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().ContainSingle();
		// Explicit left alignment: text starts at left margin
		target.DrawTextCalls[0].BaselineXTwips.Should().Be((float)section.MarginLeft);
	}

	[Fact]
	public void EmitPage_CenterAlignedParagraph_TextCentered()
	{
		var paragraph = new Paragraph(
			new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
			new Run(new Text("Hi")));
		var section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 };
		var contentWidth = section.PageWidth - section.MarginLeft - section.MarginRight;
		var page = TestLayoutPageFactory.Create([new LayoutBlock(new ParagraphBlock { SourceElement = paragraph, Alignment = ParagraphAlignment.Center }, 300f)], 1, section: section);
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().ContainSingle();
		var textWidth = EstimateWidth("Hi", 12f);
		var expectedX = section.MarginLeft + (contentWidth - textWidth) / 2f;
		target.DrawTextCalls[0].BaselineXTwips.Should().Be(expectedX);
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
		var page = TestLayoutPageFactory.Create([new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)], 1);
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].Text.Should().Be("Controlled text");
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
		var page = TestLayoutPageFactory.Create([new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)], 1);
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		// Both text segments should be rendered (may merge into one if same font)
		var allText = string.Concat(target.DrawTextCalls.Select(c => c.Text));
		allText.Should().Contain("Normal ");
		allText.Should().Contain("SDT");
	}

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

	private sealed class FakeRenderTarget : IRenderTarget
	{
		public List<DrawTextCall> DrawTextCalls { get; } = [];
		public List<DrawRectCall> DrawRectCalls { get; } = [];
		public List<HyperlinkCall> HyperlinkCalls { get; } = [];
		public List<NamedDestinationCall> NamedDestinationCalls { get; } = [];
		public List<RotatedTextCall> RotatedTextCalls { get; } = [];
		public List<RotatedImageCall> RotatedImageCalls { get; } = [];
		public List<DrawLineCall> DrawLineCalls { get; } = [];
		public List<string> CallOrder { get; } = [];

		public void DrawText(string text, float baselineXTwips, float baselineYTwips, RenderFont font, RenderBrush brush)
		{
			DrawTextCalls.Add(new DrawTextCall(text, baselineXTwips, baselineYTwips, font, brush));
			CallOrder.Add($"DrawText:{text}");
		}

		public void DrawLine(RenderPoint from, RenderPoint to, RenderStroke stroke)
		{
			DrawLineCalls.Add(new DrawLineCall(from, to, stroke));
		}

		public void DrawRect(RenderRect rect, RenderBrush? fill, RenderStroke? stroke)
		{
			DrawRectCalls.Add(new DrawRectCall(rect, fill, stroke));
		}

		public void DrawImage(ImageData image, RenderRect rect)
		{
			// Not needed by these tests.
		}

		public void DrawPath(string pathData, RenderBrush? fill, RenderStroke? stroke)
		{
			// Not needed by these tests.
		}

		public void PushClip(RenderRect clipRect)
		{
			// Not needed by these tests.
		}

		public void PopClip()
		{
			// Not needed by these tests.
		}

		public void SetHyperlink(RenderRect rect, string uri)
		{
			HyperlinkCalls.Add(new HyperlinkCall(rect, uri));
		}

		public void SetNamedDestination(string name, float xTwips, float yTwips)
		{
			NamedDestinationCalls.Add(new NamedDestinationCall(name, xTwips, yTwips));
		}

		public void DrawRotatedText(string text, float centerXTwips, float centerYTwips, float rotationDegrees, RenderFont font, RenderBrush brush)
		{
			RotatedTextCalls.Add(new RotatedTextCall(text, centerXTwips, centerYTwips, rotationDegrees, font, brush));
			CallOrder.Add($"DrawRotatedText:{text}");
		}

		public void DrawRotatedImage(ImageData image, float centerXTwips, float centerYTwips, float widthTwips, float heightTwips, float rotationDegrees, float opacity)
		{
			RotatedImageCalls.Add(new RotatedImageCall(image, centerXTwips, centerYTwips, widthTwips, heightTwips, rotationDegrees, opacity));
			CallOrder.Add("DrawRotatedImage");
		}
	}

	private readonly record struct DrawTextCall(
		string Text,
		float BaselineXTwips,
		float BaselineYTwips,
		RenderFont Font,
		RenderBrush Brush);

	private readonly record struct DrawRectCall(
		RenderRect Rect,
		RenderBrush? Fill,
		RenderStroke? Stroke);

	private readonly record struct HyperlinkCall(RenderRect Rect, string Uri);

	private readonly record struct NamedDestinationCall(string Name, float XTwips, float YTwips);

	private readonly record struct RotatedTextCall(string Text, float CenterXTwips, float CenterYTwips, float RotationDegrees, RenderFont Font, RenderBrush Brush);

	private readonly record struct RotatedImageCall(ImageData Image, float CenterXTwips, float CenterYTwips, float WidthTwips, float HeightTwips, float RotationDegrees, float Opacity);

	private readonly record struct DrawLineCall(RenderPoint From, RenderPoint To, RenderStroke Stroke);
}
