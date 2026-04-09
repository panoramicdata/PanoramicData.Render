namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed class RenderCommandEmitterTests
{
	[Fact]
	public void EmitPage_ParagraphBlock_EmitsSingleDrawTextCommand()
	{
		var section = new SectionInfo
		{
			MarginLeft = 1000,
			MarginRight = 1000,
			PageWidth = 12000
		};
		var paragraph = new ParagraphBlock
		{
			SourceElement = new Paragraph(new Run(new Text("Hello world")))
		};
		var page = new LayoutPage
		{
			Section = section,
			PageNumber = 1,
			ContentTopTwips = 1200,
			Blocks =
			[
				new LayoutBlock(paragraph, 400f)
			]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].Text.Should().Be("Hello world");
		target.DrawTextCalls[0].BaselineXTwips.Should().Be(1000f);
		target.DrawTextCalls[0].BaselineYTwips.Should().Be(1440f);
		target.DrawTextCalls[0].Font.Family.Should().Be("Times New Roman");
	}

	[Fact]
	public void EmitPage_WithFallbackFontFamily_UsesConfiguredFont()
	{
		var paragraph = new ParagraphBlock
		{
			SourceElement = new Paragraph(new Run(new Text("Configured")))
		};
		var page = new LayoutPage
		{
			Section = new SectionInfo { MarginLeft = 500, MarginRight = 500, PageWidth = 10000 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(paragraph, 300f)]
		};
		var target = new FakeRenderTarget();
		var options = new RenderOptions { FallbackFontFamily = "Calibri" };

		RenderCommandEmitter.EmitPage(page, target, options);

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].Font.Family.Should().Be("Calibri");
	}

	[Fact]
	public void EmitPage_TablePlaceholderBlock_EmitsDrawRectCommand()
	{
		var page = new LayoutPage
		{
			Section = new SectionInfo { MarginLeft = 900, MarginRight = 1100, PageWidth = 15000 },
			PageNumber = 1,
			ContentTopTwips = 1400,
			Blocks =
			[
				new LayoutBlock(new TablePlaceholderBlock
				{
					TableElement = new Table()
				}, 800f)
			]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawRectCalls.Should().ContainSingle();
		target.DrawRectCalls[0].Rect.XTwips.Should().Be(900f);
		target.DrawRectCalls[0].Rect.YTwips.Should().Be(1400f);
		target.DrawRectCalls[0].Rect.WidthTwips.Should().Be(13000f);
		target.DrawRectCalls[0].Rect.HeightTwips.Should().Be(800f);
	}

	[Fact]
	public void EmitDocument_MultiplePages_EmitsCommandsAcrossPages()
	{
		var p1 = new LayoutPage
		{
			Section = new SectionInfo { MarginLeft = 720, MarginRight = 720, PageWidth = 12240 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks =
			[
				new LayoutBlock(new ParagraphBlock
				{
					SourceElement = new Paragraph(new Run(new Text("First")))
				}, 300f)
			]
		};
		var p2 = new LayoutPage
		{
			Section = new SectionInfo { MarginLeft = 720, MarginRight = 720, PageWidth = 12240 },
			PageNumber = 2,
			ContentTopTwips = 1000,
			Blocks =
			[
				new LayoutBlock(new ParagraphBlock
				{
					SourceElement = new Paragraph(new Run(new Text("Second")))
				}, 300f)
			]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitDocument([p1, p2], target);

		target.DrawTextCalls.Should().HaveCount(2);
		target.DrawTextCalls[0].Text.Should().Be("First");
		target.DrawTextCalls[1].Text.Should().Be("Second");
	}

	private sealed class FakeRenderTarget : IRenderTarget
	{
		public List<DrawTextCall> DrawTextCalls { get; } = [];
		public List<DrawRectCall> DrawRectCalls { get; } = [];

		public void DrawText(string text, float baselineXTwips, float baselineYTwips, RenderFont font, RenderBrush brush)
		{
			DrawTextCalls.Add(new DrawTextCall(text, baselineXTwips, baselineYTwips, font, brush));
		}

		public void DrawLine(RenderPoint from, RenderPoint to, RenderStroke stroke)
		{
		}

		public void DrawRect(RenderRect rect, RenderBrush? fill, RenderStroke? stroke)
		{
			DrawRectCalls.Add(new DrawRectCall(rect, fill, stroke));
		}

		public void DrawImage(ImageData image, RenderRect rect)
		{
		}

		public void DrawPath(string pathData, RenderBrush? fill, RenderStroke? stroke)
		{
		}

		public void PushClip(RenderRect clipRect)
		{
		}

		public void PopClip()
		{
		}

		public void SetHyperlink(RenderRect rect, string uri)
		{
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
}
