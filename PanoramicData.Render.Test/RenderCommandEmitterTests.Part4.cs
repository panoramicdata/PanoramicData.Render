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
