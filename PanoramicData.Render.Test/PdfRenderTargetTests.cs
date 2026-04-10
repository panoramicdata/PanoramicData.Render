namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;

public sealed class PdfRenderTargetTests
{
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
}
