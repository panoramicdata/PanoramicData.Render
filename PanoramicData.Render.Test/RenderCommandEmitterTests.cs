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
	public void EmitPage_AdjacentRunsWithSameFormatting_MergesIntoSingleDrawText()
	{
		var paragraph = new ParagraphBlock
		{
			SourceElement = new Paragraph(
				new Run(new Text("Hel")),
				new Run(new Text("lo")))
		};
		var page = new LayoutPage
		{
			Section = new SectionInfo { MarginLeft = 500, MarginRight = 500, PageWidth = 10000 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(paragraph, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].Text.Should().Be("Hello");
	}

	[Fact]
	public void EmitPage_RunsWithDifferentFormatting_EmitSeparateDrawTextCommands()
	{
		var paragraph = new ParagraphBlock
		{
			SourceElement = new Paragraph(
				new Run(new Text("A")),
				new Run(new RunProperties(new Bold()), new Text("B")))
		};
		var page = new LayoutPage
		{
			Section = new SectionInfo { MarginLeft = 500, MarginRight = 500, PageWidth = 10000 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(paragraph, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().HaveCount(2);
		target.DrawTextCalls[0].Text.Should().Be("A");
		target.DrawTextCalls[1].Text.Should().Be("B");
		target.DrawTextCalls[0].Font.IsBold.Should().BeFalse();
		target.DrawTextCalls[1].Font.IsBold.Should().BeTrue();
		target.DrawTextCalls[1].BaselineXTwips.Should().BeGreaterThan(target.DrawTextCalls[0].BaselineXTwips);
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

	[Fact]
	public void EmitPage_ComplexPageField_RendersComputedCurrentPage()
	{
		var paragraph = new Paragraph(
			new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
			new Run(new FieldCode(" PAGE ")),
			new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }),
			new Run(new Text("999")),
			new Run(new FieldChar { FieldCharType = FieldCharValues.End }));
		var page = new LayoutPage
		{
			Section = new SectionInfo { MarginLeft = 500, MarginRight = 500, PageWidth = 10000 },
			PageNumber = 7,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target, totalPageCount: 13, renderTimestampUtc: new DateTime(2026, 4, 10, 11, 30, 0, DateTimeKind.Utc));

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].Text.Should().Be("7");
	}

	[Fact]
	public void EmitPage_ComplexNumPagesField_RendersProvidedTotalPages()
	{
		var paragraph = new Paragraph(
			new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
			new Run(new FieldCode(" NUMPAGES ")),
			new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }),
			new Run(new Text("1")),
			new Run(new FieldChar { FieldCharType = FieldCharValues.End }));
		var page = new LayoutPage
		{
			Section = new SectionInfo { MarginLeft = 500, MarginRight = 500, PageWidth = 10000 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target, totalPageCount: 42, renderTimestampUtc: new DateTime(2026, 4, 10, 11, 30, 0, DateTimeKind.Utc));

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].Text.Should().Be("42");
	}

	[Fact]
	public void EmitPage_SimpleDateField_RendersUsingTimestamp()
	{
		var paragraph = new Paragraph(
			new SimpleField(new Run(new Text("stale")))
			{
				Instruction = " DATE "
			});
		var page = new LayoutPage
		{
			Section = new SectionInfo { MarginLeft = 500, MarginRight = 500, PageWidth = 10000 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};
		var target = new FakeRenderTarget();
		var timestamp = new DateTime(2026, 4, 10, 11, 30, 0, DateTimeKind.Utc);

		RenderCommandEmitter.EmitPage(page, target, renderTimestampUtc: timestamp);

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].Text.Should().Be(timestamp.ToString("d", System.Globalization.CultureInfo.InvariantCulture));
	}

	[Fact]
	public void EmitPage_SimpleTocField_RendersCachedResultText()
	{
		var paragraph = new Paragraph(
			new SimpleField(new Run(new Text("Heading 1........1")))
			{
				Instruction = " TOC "
			});
		var page = new LayoutPage
		{
			Section = new SectionInfo { MarginLeft = 500, MarginRight = 500, PageWidth = 10000 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].Text.Should().Be("Heading 1........1");
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
