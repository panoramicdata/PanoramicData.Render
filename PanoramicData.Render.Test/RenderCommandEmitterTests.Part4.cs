namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

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

		AssertSingleText(EmitParagraph(paragraph), "مرحبا");
	}

	[Fact]
	public void EmitPage_BiDiParagraphNoAlignment_DefaultsToRightAligned()
	{
		var target = EmitAlignedParagraph(AlignedParagraph("Test", new BiDi()), isBiDi: true);

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].BaselineXTwips.Should().Be(720f + ContentWidthTwips - EstimateWidth("Test", 12f));
	}

	[Fact]
	public void EmitPage_BiDiParagraphExplicitLeftAlignment_LeftAligned()
	{
		var paragraph = AlignedParagraph("Test", new BiDi(), new Justification { Val = JustificationValues.Left });
		var target = EmitAlignedParagraph(paragraph, isBiDi: true, alignment: ParagraphAlignment.Left);

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].BaselineXTwips.Should().Be(720f);
	}

	[Fact]
	public void EmitPage_CenterAlignedParagraph_TextCentered()
	{
		var paragraph = AlignedParagraph("Hi", new Justification { Val = JustificationValues.Center });
		var target = EmitAlignedParagraph(paragraph, alignment: ParagraphAlignment.Center);

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].BaselineXTwips.Should().Be(720f + (ContentWidthTwips - EstimateWidth("Hi", 12f)) / 2f);
	}

	[Fact]
	public void EmitPage_InlineSdtRun_RendersInnerContent()
	{
		// An inline content control (SdtRun) wrapping a run should render the inner text
		AssertSingleText(EmitParagraph(new Paragraph(SdtRunOf("Controlled text"))), "Controlled text");
	}

	[Fact]
	public void EmitPage_SdtRunMixedWithNormalRun_RendersBoth()
	{
		var paragraph = new Paragraph(
			new Run(new Text("Normal ") { Space = SpaceProcessingModeValues.Preserve }),
			SdtRunOf("SDT"));
		var target = EmitParagraph(paragraph);

		// Both text segments should be rendered (may merge into one if same font)
		var allText = string.Concat(target.DrawTextCalls.Select(c => c.Text));
		allText.Should().Contain("Normal ");
		allText.Should().Contain("SDT");
	}

}
