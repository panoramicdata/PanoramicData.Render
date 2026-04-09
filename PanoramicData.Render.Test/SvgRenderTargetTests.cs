namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;

public sealed class SvgRenderTargetTests
{
	[Fact]
	public void BuildSvg_EmptyTarget_HasSvgRootAndViewBox()
	{
		var target = new SvgRenderTarget(12240f, 15840f);

		var svg = target.BuildSvg();

		svg.Should().Contain("<svg");
		svg.Should().Contain("viewBox=\"0 0 12240 15840\"");
		svg.Should().Contain("</svg>");
	}

	[Fact]
	public void DrawText_WritesTextElement()
	{
		var target = new SvgRenderTarget(1000f, 1000f);

		target.DrawText(
			"Hello",
			100f,
			200f,
			new RenderFont("Calibri", 11f, IsBold: true),
			new SolidRenderBrush(new RenderColor(255, 0, 0)));

		var svg = target.BuildSvg();

		svg.Should().Contain("<text");
		svg.Should().Contain("x=\"100\"");
		svg.Should().Contain("y=\"200\"");
		svg.Should().Contain("font-family=\"Calibri\"");
		svg.Should().Contain("font-weight=\"bold\"");
		svg.Should().Contain(">Hello</text>");
	}

	[Fact]
	public void DrawRect_WithFillAndStroke_WritesRectAttributes()
	{
		var target = new SvgRenderTarget(1000f, 1000f);

		target.DrawRect(
			new RenderRect(10f, 20f, 300f, 400f),
			new SolidRenderBrush(new RenderColor(0, 128, 255)),
			new RenderStroke(new RenderColor(10, 20, 30), 12f));

		var svg = target.BuildSvg();

		svg.Should().Contain("<rect");
		svg.Should().Contain("x=\"10\"");
		svg.Should().Contain("y=\"20\"");
		svg.Should().Contain("width=\"300\"");
		svg.Should().Contain("height=\"400\"");
		svg.Should().Contain("fill=\"#0080FF\"");
		svg.Should().Contain("stroke=\"#0A141E\"");
		svg.Should().Contain("stroke-width=\"12\"");
	}

	[Fact]
	public void PushClip_DrawRect_AppliesClipPath()
	{
		var target = new SvgRenderTarget(1000f, 1000f);

		target.PushClip(new RenderRect(0f, 0f, 100f, 100f));
		target.DrawRect(new RenderRect(5f, 5f, 10f, 10f), null, null);

		var svg = target.BuildSvg();

		svg.Should().Contain("<defs>");
		svg.Should().Contain("<clipPath id=\"clip1\">");
		svg.Should().Contain("clip-path=\"url(#clip1)\"");
	}
}
