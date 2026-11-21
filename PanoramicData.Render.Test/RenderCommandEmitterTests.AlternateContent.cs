namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using static PanoramicData.Render.Test.EmitterBuilders;
using static PanoramicData.Render.Test.EmitterBuilders2;

public sealed partial class RenderCommandEmitterTests
{
	private static List<string> EmitParagraphText(Paragraph paragraph)
	{
		var page = TestLayoutPageFactory.Create([new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)], 1);
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		return [.. target.DrawTextCalls.Select(c => c.Text)];
	}

	[Fact]
	public void EmitPage_ParagraphWithAlternateContentTextBox_DoesNotLeakTextBoxText()
	{
		var textBox = new TextBoxContent(new Paragraph(new Run(new Text("Hidden box text"))));
		var paragraph = new Paragraph(new Run(new AlternateContent(new AlternateContentChoice(textBox), new AlternateContentFallback())));

		EmitParagraphText(paragraph).Should().NotContain(t => t.Contains("Hidden box text"));
	}

	[Fact]
	public void EmitPage_ParagraphWithTextBoxAndDirectText_KeepsOnlyOwnText()
	{
		var textBox = new TextBoxContent(new Paragraph(new Run(new Text("Hidden box text"))));
		var paragraph = new Paragraph(new Run(new AlternateContent(new AlternateContentChoice(textBox))), new Text("Own text"));

		var drawn = EmitParagraphText(paragraph);

		drawn.Should().Contain(t => t.Contains("Own text"));
		drawn.Should().NotContain(t => t.Contains("Hidden box text"));
	}

	[Fact]
	public void EmitPage_ParagraphWithOnlyDirectText_StillFallsBackToInnerText()
	{
		var paragraph = new Paragraph(new Text("Direct text"));

		EmitParagraphText(paragraph).Should().Contain(t => t.Contains("Direct text"));
	}
}