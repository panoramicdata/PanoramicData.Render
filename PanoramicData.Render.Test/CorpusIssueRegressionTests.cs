namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;

public sealed class CorpusIssueRegressionTests
{
	[Theory]
	[InlineData("inline-images")]
	[InlineData("floating-images")]
	[InlineData("panoramic-data-document-2026")]
	public void CorpusDocument_PageCount_MatchesReference(string stem)
	{
		var assetsDir = GetAssetsDirectory();
		var docPath = ResolvePath(Path.Combine(assetsDir, "docx"), stem);
		using var stream = File.OpenRead(docPath);
		var result = new DocxRenderer(new RenderOptions()).Render(stream);
		var expected = Directory.GetFiles(Path.Combine(assetsDir, "reference"), stem + "_page-*.png", SearchOption.TopDirectoryOnly).Length;
		result.Pages.Count.Should().Be(expected);
	}

	private static string GetAssetsDirectory()
	{
		var current = new DirectoryInfo(AppContext.BaseDirectory);
		while (current is not null)
		{
			if (File.Exists(Path.Combine(current.FullName, "PanoramicData.Render.slnx")))
			{
				return Path.Combine(current.FullName, "PanoramicData.Render.Test", "test-assets");
			}
			current = current.Parent;
		}
		throw new DirectoryNotFoundException();
	}

	private static string ResolvePath(string docxDir, string stem)
	{
		var docxPath = Path.Combine(docxDir, stem + ".docx");
		if (File.Exists(docxPath))
		{
			return docxPath;
		}

		return Path.Combine(docxDir, stem + ".dotx");
	}
}
