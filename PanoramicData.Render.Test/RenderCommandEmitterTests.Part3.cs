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
	public void EmitPage_NestedListLabel_IsPositionedLeftOfBodyText()
	{
		var page = TestLayoutPageFactory.Create([
				new LayoutBlock(new ParagraphBlock
				{
					SourceElement = new Paragraph(new Run(new Text("Nested text"))),
					NumberingId = 1,
					NumberingLevel = 1
				}, 300f)
			], 1, section: new SectionInfo { MarginLeft = 720, MarginRight = 720, PageWidth = 12240 });
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().HaveCount(2);
		var labelCall = target.DrawTextCalls[0];
		var bodyCall = target.DrawTextCalls[1];
		labelCall.Text.Should().EndWith(" ");
		labelCall.BaselineXTwips.Should().BeLessThan(bodyCall.BaselineXTwips);
	}

	[Fact]
	public void EmitDocument_RestartRule_RestartsNestedLevelAfterParentIncrement()
	{
		var page = TestLayoutPageFactory.Create([
				new LayoutBlock(new ParagraphBlock { SourceElement = new Paragraph(new Run(new Text("P1"))), NumberingId = 1, NumberingLevel = 0 }, 300f),
				new LayoutBlock(new ParagraphBlock { SourceElement = new Paragraph(new Run(new Text("C1"))), NumberingId = 1, NumberingLevel = 1 }, 300f),
				new LayoutBlock(new ParagraphBlock { SourceElement = new Paragraph(new Run(new Text("C2"))), NumberingId = 1, NumberingLevel = 1 }, 300f),
				new LayoutBlock(new ParagraphBlock { SourceElement = new Paragraph(new Run(new Text("P2"))), NumberingId = 1, NumberingLevel = 0 }, 300f),
				new LayoutBlock(new ParagraphBlock { SourceElement = new Paragraph(new Run(new Text("C3"))), NumberingId = 1, NumberingLevel = 1 }, 300f)
			], 1, section: new SectionInfo { MarginLeft = 720, MarginRight = 720, PageWidth = 12240 });
		var options = new RenderOptions
		{
			NumberingStyles =
			{
				["1:0"] = new NumberingLevelStyle { LevelIndex = 0, Start = 1, NumberFormat = "decimal", LevelText = "%1." },
				["1:1"] = new NumberingLevelStyle { LevelIndex = 1, Start = 1, NumberFormat = "decimal", LevelText = "%1.%2.", RestartAfterLevel = 1 }
			}
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitDocument([page], target, options);

		target.DrawTextCalls.Select(call => call.Text).Should().ContainInOrder(
			"1. ", "P1",
			"1.1. ", "C1",
			"1.2. ", "C2",
			"2. ", "P2",
			"2.1. ", "C3");
	}

	[Fact]
	public void EmitPage_BulletStyle_UsesConfiguredBulletFontFamily()
	{
		var page = TestLayoutPageFactory.Create([
				new LayoutBlock(new ParagraphBlock
				{
					SourceElement = new Paragraph(new Run(new Text("Bullet item"))),
					NumberingId = 5,
					NumberingLevel = 0
				}, 300f)
			], 1, section: new SectionInfo { MarginLeft = 720, MarginRight = 720, PageWidth = 12240 });
		var options = new RenderOptions
		{
			NumberingStyles =
			{
				["5:0"] = new NumberingLevelStyle { LevelIndex = 0, Start = 1, NumberFormat = "bullet", LevelText = "%1", FontFamily = "Symbol" }
			}
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target, options);

		target.DrawTextCalls.Should().HaveCount(2);
		target.DrawTextCalls[0].Text.Should().Be("• ");
		target.DrawTextCalls[0].Font.Family.Should().Be("Symbol");
	}

	[Fact]
	public void EmitPage_WithTextWatermark_DrawsRotatedTextAtCenter()
	{
		var paragraph = new Paragraph(new Run(new Text("Body text")));
		var page = TestLayoutPageFactory.Create([new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)], 1, watermark: new WatermarkInfo
			{
				Kind = WatermarkKind.Text,
				Text = "DRAFT",
				FontFamily = "Calibri",
				FillColor = "silver",
				Opacity = 0.5f,
				RotationDegrees = 315f,
				WidthTwips = 10557f,
				HeightTwips = 2639f,
				IsHorizontallyCentered = true,
				IsVerticallyCentered = true
			});
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.RotatedTextCalls.Should().ContainSingle();
		target.RotatedTextCalls[0].Text.Should().Be("DRAFT");
		target.RotatedTextCalls[0].CenterXTwips.Should().Be(12240f / 2f);
		target.RotatedTextCalls[0].CenterYTwips.Should().Be(15840f / 2f);
		target.RotatedTextCalls[0].RotationDegrees.Should().Be(315f);
		target.RotatedTextCalls[0].Font.Family.Should().Be("Calibri");
		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].Text.Should().Be("Body text");
	}

	[Fact]
	public void EmitPage_NoWatermark_DoesNotDrawRotatedText()
	{
		var paragraph = new Paragraph(new Run(new Text("Body text")));
		var page = TestLayoutPageFactory.Create([new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)], 1);
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.RotatedTextCalls.Should().BeEmpty();
	}

	[Fact]
	public void EmitPage_WithImageWatermark_DrawsRotatedImageAtCenter()
	{
		var paragraph = new Paragraph(new Run(new Text("Body text")));
		var imageData = new ImageData([0x89, 0x50, 0x4E, 0x47], "image/png");
		var page = TestLayoutPageFactory.Create([new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)], 1, watermark: new WatermarkInfo
			{
				Kind = WatermarkKind.Image,
				ResolvedImageData = imageData,
				Opacity = 0.3f,
				RotationDegrees = 0f,
				WidthTwips = 7200f,
				HeightTwips = 5400f,
				IsHorizontallyCentered = true,
				IsVerticallyCentered = true
			});
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.RotatedImageCalls.Should().ContainSingle();
		target.RotatedImageCalls[0].Image.Should().BeSameAs(imageData);
		target.RotatedImageCalls[0].CenterXTwips.Should().Be(12240f / 2f);
		target.RotatedImageCalls[0].CenterYTwips.Should().Be(15840f / 2f);
		target.RotatedImageCalls[0].WidthTwips.Should().Be(7200f);
		target.RotatedImageCalls[0].HeightTwips.Should().Be(5400f);
		target.RotatedImageCalls[0].Opacity.Should().Be(0.3f);
		target.RotatedTextCalls.Should().BeEmpty();
	}

	[Fact]
	public void EmitPage_ImageWatermarkWithNoResolvedData_DoesNotDraw()
	{
		var paragraph = new Paragraph(new Run(new Text("Body text")));
		var page = TestLayoutPageFactory.Create([new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)], 1, watermark: new WatermarkInfo
			{
				Kind = WatermarkKind.Image,
				ImageRelationshipId = "rId1",
				WidthTwips = 7200f,
				HeightTwips = 5400f
			});
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.RotatedImageCalls.Should().BeEmpty();
		target.RotatedTextCalls.Should().BeEmpty();
	}

	[Fact]
	public void EmitPage_TextWatermark_RenderedBeforeBodyContent()
	{
		var paragraph = new Paragraph(new Run(new Text("Body")));
		var page = TestLayoutPageFactory.Create([new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)], 1, watermark: new WatermarkInfo
			{
				Kind = WatermarkKind.Text,
				Text = "DRAFT",
				WidthTwips = 5000f
			});
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.CallOrder.Count.Should().BeGreaterThanOrEqualTo(2);
		target.CallOrder[0].Should().StartWith("DrawRotatedText:");
		target.CallOrder[1].Should().StartWith("DrawText:");
	}

	[Fact]
	public void EmitPage_ImageWatermark_RenderedBeforeBodyContent()
	{
		var paragraph = new Paragraph(new Run(new Text("Body")));
		var imageData = new ImageData([0x89, 0x50, 0x4E, 0x47], "image/png");
		var page = TestLayoutPageFactory.Create([new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)], 1, watermark: new WatermarkInfo
			{
				Kind = WatermarkKind.Image,
				ResolvedImageData = imageData,
				WidthTwips = 7200f,
				HeightTwips = 5400f
			});
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.CallOrder.Count.Should().BeGreaterThanOrEqualTo(2);
		target.CallOrder[0].Should().Be("DrawRotatedImage");
		target.CallOrder[1].Should().StartWith("DrawText:");
	}

	[Fact]
	public void EmitPage_ParagraphWithBarTab_DrawsVerticalLine()
	{
		var paragraph = new Paragraph(
			new ParagraphProperties(
				new Tabs(
					new DocumentFormat.OpenXml.Wordprocessing.TabStop { Val = TabStopValues.Bar, Position = 2880 }
				)),
			new Run(new Text("Body text")));
		var page = TestLayoutPageFactory.Create([new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)], 1);
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawLineCalls.Should().ContainSingle();
		target.DrawLineCalls[0].From.XTwips.Should().Be(720f + 2880f);
		target.DrawLineCalls[0].From.YTwips.Should().Be(1000f);
		target.DrawLineCalls[0].To.XTwips.Should().Be(720f + 2880f);
		target.DrawLineCalls[0].To.YTwips.Should().Be(1300f);
	}

	[Fact]
	public void EmitPage_ParagraphWithoutBarTab_DrawsNoLine()
	{
		var paragraph = new Paragraph(
			new ParagraphProperties(
				new Tabs(
					new DocumentFormat.OpenXml.Wordprocessing.TabStop { Val = TabStopValues.Left, Position = 2880 }
				)),
			new Run(new Text("Body text")));
		var page = TestLayoutPageFactory.Create([new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)], 1);
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawLineCalls.Should().BeEmpty();
	}

	[Fact]
	public void EmitPage_DecimalTabStop_AlignsOnDecimalPoint()
	{
		var paragraph = new Paragraph(
			new ParagraphProperties(
				new Tabs(
					new DocumentFormat.OpenXml.Wordprocessing.TabStop { Val = TabStopValues.Decimal, Position = 4320 }
				)),
			new Run(new Text("Label") { Space = SpaceProcessingModeValues.Preserve }),
			new Run(new TabChar()),
			new Run(new Text("12.50") { Space = SpaceProcessingModeValues.Preserve }));
		var page = TestLayoutPageFactory.Create([new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)], 1);
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		// Should have two text draws: "Label" and "12.50"
		target.DrawTextCalls.Should().HaveCount(2);
		target.DrawTextCalls[0].Text.Should().Be("Label");
		target.DrawTextCalls[1].Text.Should().Be("12.50");

		// The "12.50" text should be positioned so that the "." aligns at the tab stop (4320 twips from margin)
		// "12" before decimal = 2 chars * 12 * 10 = 240 twips
		// Position = 720 (margin) + 4320 (tab) - 240 (before decimal) = 4800
		var expectedX = 720f + TabStopResolver.ComputeContentStart(
			new RenderTabStop(4320f, TabStopType.Decimal),
			0f,
			EstimateWidth("12", 12f));
		target.DrawTextCalls[1].BaselineXTwips.Should().Be(expectedX);
	}

	[Fact]
	public void EmitPage_DecimalTabStop_NoDecimalInText_AlignsFullWidth()
	{
		var paragraph = new Paragraph(
			new ParagraphProperties(
				new Tabs(
					new DocumentFormat.OpenXml.Wordprocessing.TabStop { Val = TabStopValues.Decimal, Position = 4320 }
				)),
			new Run(new TabChar()),
			new Run(new Text("1234") { Space = SpaceProcessingModeValues.Preserve }));
		var page = TestLayoutPageFactory.Create([new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)], 1);
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].Text.Should().Be("1234");

		// No decimal point — entire text width treated as widthBeforeDecimal
		var expectedX = 720f + TabStopResolver.ComputeContentStart(
			new RenderTabStop(4320f, TabStopType.Decimal),
			0f,
			EstimateWidth("1234", 12f));
		target.DrawTextCalls[0].BaselineXTwips.Should().Be(expectedX);
	}

	[Fact]
	public void EmitPage_RightTabStop_AlignsTextRight()
	{
		var paragraph = new Paragraph(
			new ParagraphProperties(
				new Tabs(
					new DocumentFormat.OpenXml.Wordprocessing.TabStop { Val = TabStopValues.Right, Position = 9360 }
				)),
			new Run(new TabChar()),
			new Run(new Text("Right aligned") { Space = SpaceProcessingModeValues.Preserve }));
		var page = TestLayoutPageFactory.Create([new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)], 1);
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().ContainSingle();
		var contentWidth = EstimateWidth("Right aligned", 12f);
		var expectedX = 720f + TabStopResolver.ComputeContentStart(
			new RenderTabStop(9360f, TabStopType.Right),
			contentWidth);
		target.DrawTextCalls[0].BaselineXTwips.Should().Be(expectedX);
	}

	[Fact]
	public void EmitPage_CenterTabStop_CentersText()
	{
		var paragraph = new Paragraph(
			new ParagraphProperties(
				new Tabs(
					new DocumentFormat.OpenXml.Wordprocessing.TabStop { Val = TabStopValues.Center, Position = 4320 }
				)),
			new Run(new TabChar()),
			new Run(new Text("Centered") { Space = SpaceProcessingModeValues.Preserve }));
		var page = TestLayoutPageFactory.Create([new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)], 1);
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().ContainSingle();
		var contentWidth = EstimateWidth("Centered", 12f);
		var expectedX = 720f + TabStopResolver.ComputeContentStart(
			new RenderTabStop(4320f, TabStopType.Center),
			contentWidth);
		target.DrawTextCalls[0].BaselineXTwips.Should().Be(expectedX);
	}

	[Fact]
	public void EmitPage_LeftTabStop_PositionsAtTabStop()
	{
		var paragraph = new Paragraph(
			new ParagraphProperties(
				new Tabs(
					new DocumentFormat.OpenXml.Wordprocessing.TabStop { Val = TabStopValues.Left, Position = 2880 }
				)),
			new Run(new Text("Before") { Space = SpaceProcessingModeValues.Preserve }),
			new Run(new TabChar()),
			new Run(new Text("After") { Space = SpaceProcessingModeValues.Preserve }));
		var page = TestLayoutPageFactory.Create([new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)], 1);
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().HaveCount(2);
		target.DrawTextCalls[0].Text.Should().Be("Before");
		target.DrawTextCalls[1].Text.Should().Be("After");
		target.DrawTextCalls[1].BaselineXTwips.Should().Be(720f + 2880f);
	}

	[Fact]
	public void EmitPage_DotLeader_DrawsDotsBeforeTabStop()
	{
		var paragraph = new Paragraph(
			new ParagraphProperties(
				new Tabs(
					new DocumentFormat.OpenXml.Wordprocessing.TabStop
					{
						Val = TabStopValues.Right,
						Position = 9360,
						Leader = TabStopLeaderCharValues.Dot
					}
				)),
			new Run(new Text("Item") { Space = SpaceProcessingModeValues.Preserve }),
			new Run(new TabChar()),
			new Run(new Text("100") { Space = SpaceProcessingModeValues.Preserve }));
		var page = TestLayoutPageFactory.Create([new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)], 1);
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		// Should have "Item", multiple "." leader characters, and "100"
		target.DrawTextCalls.Count.Should().BeGreaterThan(3);
		target.DrawTextCalls[0].Text.Should().Be("Item");
		target.DrawTextCalls[^1].Text.Should().Be("100");
		target.DrawTextCalls.Where(c => c.Text == ".").Should().NotBeEmpty();
	}
}
