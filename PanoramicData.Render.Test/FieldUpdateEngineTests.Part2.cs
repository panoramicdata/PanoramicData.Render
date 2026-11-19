namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using static PanoramicData.Render.Test.FieldUpdateTestDocuments;

public sealed partial class FieldUpdateEngineTests
{
	[Fact]
	public void Apply_PageRefField_ResolvesToBookmarkPageNumber()
	{
		using var run = ApplyChanged(CreateDocxWithPageRefField(), "PAGEREF");
		var paragraphs = run.Paragraphs;
		GetParagraphResultText(paragraphs[0]).Should().Be("2");
	}

	[Fact]
	public void Apply_RefField_ResolvesToBookmarkText()
	{
		using var run = ApplyChanged(CreateDocxWithRefField(), "REF");
		var paragraphs = run.Paragraphs;
		GetParagraphResultText(paragraphs[0]).Should().Be("Target Text");
	}

	[Fact]
	public void Apply_CrossReferenceFields_WhenDisabled_DoesNotUpdate()
	{
		using var run = ApplyFieldUpdate(CreateDocxWithPageRefField(), new FieldUpdateOptions { UpdateCrossReferences = false });
		var doc = run.Doc;
		var result = run.Result;

		result.UpdatedFields.Should().NotContain("PAGEREF");

		var paragraphs = doc.DocumentBody.Elements<Paragraph>().ToArray();
		GetParagraphResultText(paragraphs[0]).Should().Be("99");
	}

	private static string GetParagraphResultText(Paragraph paragraph)
		=> string.Concat(paragraph.Descendants<Text>().Select(text => text.Text));

	private static string[] GetParagraphTextNodes(Paragraph paragraph)
		=> [.. paragraph.Descendants<Text>().Select(text => text.Text)];

	private static string? GetParagraphStyleId(Paragraph paragraph)
		=> paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;

	private static string? GetParagraphHyperlinkAnchor(Paragraph paragraph)
		=> paragraph.Descendants<Hyperlink>().FirstOrDefault()?.Anchor?.Value;

	private static string? GetHeadingBookmarkName(Paragraph paragraph)
		=> paragraph.Elements<BookmarkStart>().FirstOrDefault()?.Name?.Value;
}
