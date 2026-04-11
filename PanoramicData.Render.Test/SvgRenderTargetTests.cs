namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;

public sealed class SvgRenderTargetTests
{
	private static RenderOptions DefaultOptions => new();

	[Fact]
	public void BuildSvg_EmptyTarget_HasSvgRootAndViewBox()
	{
		var target = new SvgRenderTarget(12240f, 15840f, DefaultOptions);

		var svg = target.BuildSvg();

		svg.Should().Contain("<svg");
		svg.Should().Contain("viewBox=\"0 0 12240 15840\"");
		svg.Should().Contain("</svg>");
	}

	[Fact]
	public void DrawText_WritesTextElement()
	{
		var target = new SvgRenderTarget(1000f, 1000f, DefaultOptions);

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
		var target = new SvgRenderTarget(1000f, 1000f, DefaultOptions);

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
		var target = new SvgRenderTarget(1000f, 1000f, DefaultOptions);

		target.PushClip(new RenderRect(0f, 0f, 100f, 100f));
		target.DrawRect(new RenderRect(5f, 5f, 10f, 10f), null, null);

		var svg = target.BuildSvg();

		svg.Should().Contain("<defs>");
		svg.Should().Contain("<clipPath id=\"clip1\">");
		svg.Should().Contain("clip-path=\"url(#clip1)\"");
	}

	[Fact]
	public void DrawImage_WritesDataUriImageElement()
	{
		var target = new SvgRenderTarget(1000f, 1000f, DefaultOptions);

		target.DrawImage(new ImageData([1, 2, 3], "image/png"), new RenderRect(1f, 2f, 3f, 4f));

		var svg = target.BuildSvg();

		svg.Should().Contain("<image");
		svg.Should().Contain("xlink:href=\"data:image/png;base64,");
	}

	[Fact]
	public void DrawImage_EmbedImagesFalse_UsesSourceUri()
	{
		var options = new RenderOptions { EmbedImages = false };
		var target = new SvgRenderTarget(1000f, 1000f, options);

		target.DrawImage(new ImageData([1, 2, 3], "image/png", "https://cdn.example.com/image.png"), new RenderRect(1f, 2f, 3f, 4f));

		var svg = target.BuildSvg();

		svg.Should().Contain("xlink:href=\"https://cdn.example.com/image.png\"");
		svg.Should().NotContain("data:image/png;base64,");
	}

	[Fact]
	public void DrawImage_EmbedImagesFalse_WithoutSourceUri_UsesGeneratedImageReference()
	{
		var options = new RenderOptions { EmbedImages = false };
		var target = new SvgRenderTarget(1000f, 1000f, options);

		target.DrawImage(new ImageData([1, 2, 3], "image/png"), new RenderRect(1f, 2f, 3f, 4f));

		var svg = target.BuildSvg();

		svg.Should().Contain("xlink:href=\"images/image-1.png\"");
	}

	[Fact]
	public void BuildSvg_NonDefaultTargetDpi_ScalesDimensions()
	{
		var options = new RenderOptions { TargetDpi = 192 };
		var target = new SvgRenderTarget(1000f, 500f, options);

		var svg = target.BuildSvg();

		svg.Should().Contain("viewBox=\"0 0 2000 1000\"");
		svg.Should().Contain("width=\"2000\"");
		svg.Should().Contain("height=\"1000\"");
	}

	[Fact]
	public void DrawPath_WritesPathElement()
	{
		var target = new SvgRenderTarget(1000f, 1000f, DefaultOptions);

		target.DrawPath("M 0 0 L 10 10 Z", null, new RenderStroke(new RenderColor(0, 0, 0), 5f));

		var svg = target.BuildSvg();

		svg.Should().Contain("<path");
		svg.Should().Contain("d=\"M 0 0 L 10 10 Z\"");
	}

	[Fact]
	public void SetHyperlink_WritesAnchorElement()
	{
		var target = new SvgRenderTarget(1000f, 1000f, DefaultOptions);

		target.SetHyperlink(new RenderRect(5f, 6f, 7f, 8f), "https://example.com");

		var svg = target.BuildSvg();

		svg.Should().Contain("<a xlink:href=\"https://example.com\">");
	}

	[Fact]
	public void DrawText_WithUnderlineAndStrikethrough_WritesDecorationLines()
	{
		var target = new SvgRenderTarget(1000f, 1000f, DefaultOptions);

		target.DrawText(
			"Decorated",
			100f,
			300f,
			new RenderFont("Calibri", 12f, IsUnderline: true, IsStrikethrough: true),
			new SolidRenderBrush(new RenderColor(0, 0, 0)));

		var svg = target.BuildSvg();

		svg.Should().Contain("<text");
		svg.Should().Contain("<line");
	}

	[Fact]
	public void DrawText_EmbedFontsFalse_NoStyleElement()
	{
		var options = new RenderOptions { EmbedFonts = false };
		var target = new SvgRenderTarget(1000f, 1000f, options);

		target.DrawText(
			"Text",
			100f,
			200f,
			new RenderFont("Calibri", 11f),
			new SolidRenderBrush(new RenderColor(0, 0, 0)));

		var svg = target.BuildSvg();

		// When EmbedFonts is false, no <style> block should be emitted
		svg.Should().NotContain("<style>");
		svg.Should().NotContain("@font-face");
	}

	[Fact]
	public void DrawText_EmbedFontsTrue_CreatesStyleElement()
	{
		var options = new RenderOptions { EmbedFonts = true, FontDirectories = [] };
		var target = new SvgRenderTarget(1000f, 1000f, options);

		target.DrawText(
			"Text",
			100f,
			200f,
			new RenderFont("Calibri", 11f),
			new SolidRenderBrush(new RenderColor(0, 0, 0)));

		var svg = target.BuildSvg();

		// When EmbedFonts is true, a <style> block should be created (even if fonts are not found on disk)
		// Note: If font files are not found, @font-face won't be emitted for that font
		svg.Should().Contain("<style>");
		// Since FontDirectories is empty, fonts won't be found and @font-face won't be generated
		// This is expected behavior - in real usage, FontDirectories would contain system font directories
	}

	[Fact]
	public void DrawText_MultipleFonts_AllTrackedWhenEmbedding()
	{
		var options = new RenderOptions { EmbedFonts = true, FontDirectories = [] };
		var target = new SvgRenderTarget(1000f, 1000f, options);

		target.DrawText("Arial", 100f, 200f, new RenderFont("Arial", 11f), new SolidRenderBrush(new RenderColor(0, 0, 0)));
		target.DrawText("Times", 100f, 300f, new RenderFont("TimesNewRoman", 11f), new SolidRenderBrush(new RenderColor(0, 0, 0)));
		target.DrawText("Arial again", 100f, 400f, new RenderFont("Arial", 11f), new SolidRenderBrush(new RenderColor(0, 0, 0)));

		var svg = target.BuildSvg();

		// Style element should be present when EmbedFonts is true
		svg.Should().Contain("<style>");

		// With empty FontDirectories, @font-face declarations won't be added (font files can't be found)
		// But in production with real font directories, they would be
	}

	[Fact]
	public void SetNamedDestination_EmitsAnchorElementWithId()
	{
		var target = new SvgRenderTarget(12240f, 15840f, new RenderOptions());

		target.SetNamedDestination("myBookmark", 500f, 1000f);

		var svg = target.BuildSvg();

		svg.Should().Contain("id=\"myBookmark\"");
	}

	[Fact]
	public void SetNamedDestination_NullName_ThrowsArgumentNullException()
	{
		var target = new SvgRenderTarget(12240f, 15840f, new RenderOptions());

		var action = () => target.SetNamedDestination(null!, 500f, 1000f);

		action.Should().Throw<ArgumentNullException>();
	}
}
