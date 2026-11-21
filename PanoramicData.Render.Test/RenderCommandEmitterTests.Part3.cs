namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using RenderTabStop = PanoramicData.Render.TabStop;

using static PanoramicData.Render.Test.EmitterBuilders;
using static PanoramicData.Render.Test.EmitterBuilders2;

public sealed partial class RenderCommandEmitterTests
{
	[Fact]
	public void EmitPage_NestedListLabel_IsPositionedLeftOfBodyText()
	{
		var target = Emit(StandardPage(1, NumberedBlock("Nested text", 1, 1)));

		target.DrawTextCalls.Should().HaveCount(2);
		var labelCall = target.DrawTextCalls[0];
		var bodyCall = target.DrawTextCalls[1];
		labelCall.Text.Should().EndWith(" ");
		labelCall.BaselineXTwips.Should().BeLessThan(bodyCall.BaselineXTwips);
	}

	[Fact]
	public void EmitDocument_RestartRule_RestartsNestedLevelAfterParentIncrement()
	{
		var page = StandardPage(1, NumberedBlock("P1", 1, 0), NumberedBlock("C1", 1, 1), NumberedBlock("C2", 1, 1), NumberedBlock("P2", 1, 0), NumberedBlock("C3", 1, 1));
		var options = NumberingOptions(
			("1:0", NumberLevel(0, "%1.")),
			("1:1", NumberLevel(1, "%1.%2.", restartAfterLevel: 1)));
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitDocument([page], target, options);

		AssertTextsInOrder(target, "1. ", "P1", "1.1. ", "C1", "1.2. ", "C2", "2. ", "P2", "2.1. ", "C3");
	}

	[Fact]
	public void EmitPage_BulletStyle_UsesConfiguredBulletFontFamily()
	{
		var options = NumberingOptions(("5:0", NumberLevel(0, "%1", "bullet", "Symbol")));
		var target = Emit(StandardPage(1, NumberedBlock("Bullet item", 5, 0)), options: options);

		target.DrawTextCalls.Should().HaveCount(2);
		target.DrawTextCalls[0].Text.Should().Be("• ");
		target.DrawTextCalls[0].Font.Family.Should().Be("Symbol");
	}

	[Fact]
	public void EmitPage_WithTextWatermark_DrawsRotatedTextAtCenter()
	{
		var target = Emit(WatermarkPage(new WatermarkInfo
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
		}));

		target.RotatedTextCalls.Should().ContainSingle();
		target.RotatedTextCalls[0].Text.Should().Be("DRAFT");
		target.RotatedTextCalls[0].CenterXTwips.Should().Be(12240f / 2f);
		target.RotatedTextCalls[0].CenterYTwips.Should().Be(15840f / 2f);
		target.RotatedTextCalls[0].RotationDegrees.Should().Be(315f);
		target.RotatedTextCalls[0].Font.Family.Should().Be("Calibri");
		AssertSingleText(target, "Body text");
	}

	[Fact]
	public void EmitPage_NoWatermark_DoesNotDrawRotatedText()
	{
		EmitParagraph(new Paragraph(new Run(new Text("Body text")))).RotatedTextCalls.Should().BeEmpty();
	}

	[Fact]
	public void EmitPage_WithImageWatermark_DrawsRotatedImageAtCenter()
	{
		var imageData = PngImage();
		var target = Emit(WatermarkPage(new WatermarkInfo
		{
			Kind = WatermarkKind.Image,
			ResolvedImageData = imageData,
			Opacity = 0.3f,
			RotationDegrees = 0f,
			WidthTwips = 7200f,
			HeightTwips = 5400f,
			IsHorizontallyCentered = true,
			IsVerticallyCentered = true
		}));

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
		var target = Emit(WatermarkPage(new WatermarkInfo
		{
			Kind = WatermarkKind.Image,
			ImageRelationshipId = "rId1",
			WidthTwips = 7200f,
			HeightTwips = 5400f
		}));

		target.RotatedImageCalls.Should().BeEmpty();
		target.RotatedTextCalls.Should().BeEmpty();
	}

	[Fact]
	public void EmitPage_TextWatermark_RenderedBeforeBodyContent()
	{
		var target = Emit(WatermarkPage(new WatermarkInfo
		{
			Kind = WatermarkKind.Text,
			Text = "DRAFT",
			WidthTwips = 5000f
		}));

		AssertWatermarkFirst(target, "DrawRotatedText:");
	}

	[Fact]
	public void EmitPage_ImageWatermark_RenderedBeforeBodyContent()
	{
		var target = Emit(WatermarkPage(new WatermarkInfo
		{
			Kind = WatermarkKind.Image,
			ResolvedImageData = PngImage(),
			WidthTwips = 7200f,
			HeightTwips = 5400f
		}));

		AssertWatermarkFirst(target, "DrawRotatedImage");
	}

	[Fact]
	public void EmitPage_ParagraphWithBarTab_DrawsVerticalLine()
	{
		var target = EmitParagraph(TabbedParagraph(TabStopValues.Bar, 2880, "Body text"));

		target.DrawLineCalls.Should().ContainSingle();
		target.DrawLineCalls[0].From.XTwips.Should().Be(720f + 2880f);
		target.DrawLineCalls[0].From.YTwips.Should().Be(1000f);
		target.DrawLineCalls[0].To.XTwips.Should().Be(720f + 2880f);
		target.DrawLineCalls[0].To.YTwips.Should().Be(1300f);
	}

	[Fact]
	public void EmitPage_ParagraphWithoutBarTab_DrawsNoLine()
	{
		EmitParagraph(TabbedParagraph(TabStopValues.Left, 2880, "Body text")).DrawLineCalls.Should().BeEmpty();
	}

	[Fact]
	public void EmitPage_DecimalTabStop_AlignsOnDecimalPoint()
	{
		var target = EmitParagraph(TabbedParagraph(TabStopValues.Decimal, 4320, "Label", Tab, "12.50"));

		target.DrawTextCalls.Select(call => call.Text).Should().Equal("Label", "12.50");
		target.DrawTextCalls[1].BaselineXTwips.Should().Be(ExpectedTabX(TabStopType.Decimal, 4320f, "12"));
	}

	[Fact]
	public void EmitPage_DecimalTabStop_NoDecimalInText_AlignsFullWidth()
	{
		var target = EmitParagraph(TabbedParagraph(TabStopValues.Decimal, 4320, Tab, "1234"));

		AssertSingleText(target, "1234");
		target.DrawTextCalls[0].BaselineXTwips.Should().Be(ExpectedTabX(TabStopType.Decimal, 4320f, "1234"));
	}

	[Theory]
	[InlineData(true, 9360, "Right aligned")]
	[InlineData(false, 4320, "Centered")]
	public void EmitPage_RightAndCenterTabStops_AlignText(bool right, int position, string text)
	{
		var target = EmitParagraph(TabbedParagraph(right ? TabStopValues.Right : TabStopValues.Center, position, Tab, text));

		target.DrawTextCalls.Should().ContainSingle();
		var stopType = right ? TabStopType.Right : TabStopType.Center;
		target.DrawTextCalls[0].BaselineXTwips.Should().Be(720f + TabStopResolver.ComputeContentStart(new RenderTabStop(position, stopType), EstimateWidth(text, 12f)));
	}

	[Fact]
	public void EmitPage_LeftTabStop_PositionsAtTabStop()
	{
		var target = EmitParagraph(TabbedParagraph(TabStopValues.Left, 2880, "Before", Tab, "After"));

		target.DrawTextCalls.Select(call => call.Text).Should().Equal("Before", "After");
		target.DrawTextCalls[1].BaselineXTwips.Should().Be(720f + 2880f);
	}

	[Fact]
	public void EmitPage_DotLeader_DrawsDotsBeforeTabStop()
	{
		var paragraph = TabbedParagraph(TabStopValues.Right, 9360, "Item", Tab, "100");
		paragraph.GetFirstChild<ParagraphProperties>()!.GetFirstChild<Tabs>()!.GetFirstChild<DocumentFormat.OpenXml.Wordprocessing.TabStop>()!.Leader = TabStopLeaderCharValues.Dot;
		var target = EmitParagraph(paragraph);

		target.DrawTextCalls.Count.Should().BeGreaterThan(3);
		target.DrawTextCalls[0].Text.Should().Be("Item");
		target.DrawTextCalls[^1].Text.Should().Be("100");
		target.DrawTextCalls.Where(c => c.Text == ".").Should().NotBeEmpty();
	}
}
