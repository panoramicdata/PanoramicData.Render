namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;

public sealed class PdfRenderTargetTests
{
	private static readonly byte[] TinyPng =
	[
		137, 80, 78, 71, 13, 10, 26, 10,
		0, 0, 0, 13, 73, 72, 68, 82,
		0, 0, 0, 1, 0, 0, 0, 1,
		8, 6, 0, 0, 0, 31, 21, 196,
		137, 0, 0, 0, 13, 73, 68, 65,
		84, 120, 156, 99, 248, 255, 255, 63,
		0, 5, 254, 2, 254, 65, 201, 209,
		46, 0, 0, 0, 0, 73, 69, 78,
		68, 174, 66, 96, 130
	];

	[Fact]
	public void BuildPdf_EmptyDocument_StartsWithPdfHeader()
	{
		using var target = new PdfRenderTarget(12240f, 15840f);

		var bytes = target.BuildPdf();

		bytes.Should().NotBeEmpty();
		var header = System.Text.Encoding.ASCII.GetString(bytes, 0, Math.Min(bytes.Length, 5));
		header.Should().Be("%PDF-");
	}

	[Fact]
	public void DrawTextAndShapes_BuildPdf_ProducesNonEmptyDocument()
	{
		using var target = new PdfRenderTarget(12240f, 15840f);

		target.DrawText(
			"Hello PDF",
			1000f,
			1200f,
			new RenderFont("Calibri", 12f, IsBold: true),
			new SolidRenderBrush(new RenderColor(0, 0, 0)));
		target.DrawLine(
			new RenderPoint(1000f, 1400f),
			new RenderPoint(4000f, 1400f),
			new RenderStroke(new RenderColor(200, 0, 0), 20f));
		target.DrawRect(
			new RenderRect(1000f, 1600f, 3000f, 1000f),
			new SolidRenderBrush(new RenderColor(20, 120, 240, 200)),
			new RenderStroke(new RenderColor(0, 0, 0), 10f));

		var bytes = target.BuildPdf();

		bytes.Length.Should().BeGreaterThan(500);
	}

	[Fact]
	public void PushClip_DrawRect_PopClip_BuildPdf_Succeeds()
	{
		using var target = new PdfRenderTarget(8000f, 8000f);

		target.PushClip(new RenderRect(1000f, 1000f, 2000f, 2000f));
		target.DrawRect(
			new RenderRect(500f, 500f, 4000f, 4000f),
			new SolidRenderBrush(new RenderColor(0, 200, 0)),
			null);
		target.PopClip();

		var bytes = target.BuildPdf();

		bytes.Should().NotBeEmpty();
	}

	[Fact]
	public void DrawImage_ValidPng_BuildPdf_ProducesNonEmptyDocument()
	{
		using var target = new PdfRenderTarget(12240f, 15840f);

		target.DrawImage(new ImageData(TinyPng, "image/png"), new RenderRect(1000f, 1000f, 2000f, 2000f));

		var bytes = target.BuildPdf();

		bytes.Length.Should().BeGreaterThan(500);
	}

	[Fact]
	public void SetHyperlink_ExternalUrl_BuildPdf_ProducesValidPdf()
	{
		using var target = new PdfRenderTarget(12240f, 15840f);

		target.DrawText("Click here", 1000f, 1200f, new RenderFont("Calibri", 12f), new SolidRenderBrush(new RenderColor(0, 0, 255)));
		target.SetHyperlink(new RenderRect(1000f, 1000f, 2000f, 300f), "https://example.com");

		var bytes = target.BuildPdf();

		bytes.Length.Should().BeGreaterThan(500);
	}

	[Fact]
	public void SetHyperlink_InternalBookmark_BuildPdf_ProducesValidPdf()
	{
		using var target = new PdfRenderTarget(12240f, 15840f);

		target.DrawText("Jump to section", 1000f, 1200f, new RenderFont("Calibri", 12f), new SolidRenderBrush(new RenderColor(0, 0, 255)));
		target.SetHyperlink(new RenderRect(1000f, 1000f, 2000f, 300f), "#myBookmark");

		var bytes = target.BuildPdf();

		bytes.Length.Should().BeGreaterThan(500);
	}

	[Fact]
	public void SetHyperlink_NullUri_ThrowsArgumentNullException()
	{
		using var target = new PdfRenderTarget(12240f, 15840f);

		var action = () => target.SetHyperlink(new RenderRect(1000f, 1000f, 2000f, 300f), null!);

		action.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void SetNamedDestination_BuildPdf_ProducesValidPdf()
	{
		using var target = new PdfRenderTarget(12240f, 15840f);

		target.SetNamedDestination("myBookmark", 1000f, 2000f);

		var bytes = target.BuildPdf();

		bytes.Length.Should().BeGreaterThan(100);
	}

	[Fact]
	public void SetNamedDestination_NullName_ThrowsArgumentNullException()
	{
		using var target = new PdfRenderTarget(12240f, 15840f);

		var action = () => target.SetNamedDestination(null!, 1000f, 2000f);

		action.Should().Throw<ArgumentNullException>();
	}
}
