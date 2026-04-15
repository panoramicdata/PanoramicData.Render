using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using PanoramicData.Render;
using Xunit;
using Xunit.Sdk;

namespace PanoramicData.Render.Test;

public class TableBandingIntegrationTests
{
	[Fact]
	public void RenderDocument_TableWithBandedRowsStyle_HasShading()
	{
		// Render the actual panoramic document
		var testAssetDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "test-assets", "docx");
		var docxPath = Path.Combine(testAssetDir, "panoramic-data-document-2026.dotx");
		using var stream = File.OpenRead(docxPath);

		var renderer = new DocxRenderer(new RenderOptions());
		var result = renderer.Render(stream);

		// Get the first page
		var firstPage = result.Pages[0];
		var svgString = firstPage.ToSvg();

		// The table should have rect elements with fill colors (backgrounds)
		// The PanoramicData table style has band1Horz with fill="F7CAAC"
		Assert.Contains("F7CAAC", svgString); // The light peach color for banding

		// The base whole-table shading from GridTable5Dark-Accent2 (FBE4D5) should appear on
		// band2/unbanded rows now that Resolve() collects the style chain's whole-table tcPr.
		Assert.Contains("FBE4D5", svgString); // The lighter peach base background
	}
}
