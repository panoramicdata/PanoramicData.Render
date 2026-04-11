namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

/// <summary>
/// Tests for parsing <c>w:hyperlink</c> elements from DOCX paragraphs.
/// Covers step 7.5.2.
/// </summary>
public sealed class HyperlinkParsingTests
{
	[Fact]
	public void ParseParagraphRuns_ExternalHyperlink_ResolvesUri()
	{
		using var stream = TestDocxBuilder.CreateDocxWithExternalHyperlink("https://example.com");
		using var doc = DocxDocument.Load(stream);

		var paragraph = doc.DocumentBody.Elements<Paragraph>().First();
		var runs = RunElementParser.ParseParagraphRuns(paragraph, doc.MainDocumentPart);

		runs.Should().ContainSingle();
		runs[0].HyperlinkUri.Should().Be("https://example.com");
		runs[0].Elements.Should().ContainSingle()
			.Which.Should().BeOfType<TextRunElement>()
			.Which.Text.Should().Be("Click here");
	}

	[Fact]
	public void ParseParagraphRuns_InternalBookmarkHyperlink_ResolvedAsAnchor()
	{
		using var stream = TestDocxBuilder.CreateDocxWithInternalBookmarkHyperlink("Chapter1");
		using var doc = DocxDocument.Load(stream);

		var paragraphs = doc.DocumentBody.Elements<Paragraph>().ToList();
		var runs = RunElementParser.ParseParagraphRuns(paragraphs[1], doc.MainDocumentPart);

		runs.Should().ContainSingle();
		runs[0].HyperlinkUri.Should().Be("#Chapter1");
		runs[0].Elements.Should().ContainSingle()
			.Which.Should().BeOfType<TextRunElement>()
			.Which.Text.Should().Be("Go to bookmark");
	}

	[Fact]
	public void ParseParagraphRuns_MixedHyperlinksAndPlainRuns_PreservesOrder()
	{
		using var stream = TestDocxBuilder.CreateDocxWithMixedHyperlinks();
		using var doc = DocxDocument.Load(stream);

		var paragraph = doc.DocumentBody.Elements<Paragraph>().First();
		var runs = RunElementParser.ParseParagraphRuns(paragraph, doc.MainDocumentPart);

		runs.Should().HaveCount(5);

		// "Before " — plain run
		runs[0].HyperlinkUri.Should().BeNull();
		runs[0].Elements[0].Should().BeOfType<TextRunElement>()
			.Which.Text.Should().Be("Before ");

		// "external link" — external hyperlink
		runs[1].HyperlinkUri.Should().Be("https://example.com");
		runs[1].Elements[0].Should().BeOfType<TextRunElement>()
			.Which.Text.Should().Be("external link");

		// " middle " — plain run
		runs[2].HyperlinkUri.Should().BeNull();

		// "internal link" — bookmark hyperlink
		runs[3].HyperlinkUri.Should().Be("#Section1");

		// " after" — plain run
		runs[4].HyperlinkUri.Should().BeNull();
	}

	[Fact]
	public void ParseParagraphRuns_HyperlinkWithoutPart_ReturnsNullUri()
	{
		// When no OpenXmlPart is provided, external hyperlinks can't be resolved
		var paragraph = new Paragraph(
			new Hyperlink(new Run(new Text("Link"))) { Id = "rId99" });

		var runs = RunElementParser.ParseParagraphRuns(paragraph);

		runs.Should().ContainSingle();
		runs[0].HyperlinkUri.Should().BeNull();
		runs[0].Elements[0].Should().BeOfType<TextRunElement>()
			.Which.Text.Should().Be("Link");
	}

	[Fact]
	public void ParseParagraphRuns_AnchorLinkWithoutPart_StillResolvesAnchor()
	{
		// Anchor-based hyperlinks don't need the part
		var paragraph = new Paragraph(
			new Hyperlink(new Run(new Text("Link"))) { Anchor = "Section2" });

		var runs = RunElementParser.ParseParagraphRuns(paragraph);

		runs.Should().ContainSingle();
		runs[0].HyperlinkUri.Should().Be("#Section2");
	}

	[Fact]
	public void ParseParagraphRuns_HyperlinkWithNoIdOrAnchor_ReturnsNullUri()
	{
		var paragraph = new Paragraph(
			new Hyperlink(new Run(new Text("Broken link"))));

		var runs = RunElementParser.ParseParagraphRuns(paragraph);

		runs.Should().ContainSingle();
		runs[0].HyperlinkUri.Should().BeNull();
	}

	[Fact]
	public void ParseParagraphRuns_HyperlinkWithMultipleInnerRuns_AllCarryUri()
	{
		using var stream = TestDocxBuilder.CreateDocxWithExternalHyperlink("https://example.org");
		using var doc = DocxDocument.Load(stream);

		// Manually construct a hyperlink with multiple inner runs
		var paragraph = new Paragraph(
			new Hyperlink(
				new Run(new Text("First ")),
				new Run(new Text("Second")))
			{ Anchor = "MultiRun" });

		var runs = RunElementParser.ParseParagraphRuns(paragraph);

		runs.Should().HaveCount(2);
		runs[0].HyperlinkUri.Should().Be("#MultiRun");
		runs[1].HyperlinkUri.Should().Be("#MultiRun");
	}

	[Fact]
	public void ParseParagraphRuns_PlainRunHasNullHyperlinkUri()
	{
		var paragraph = new Paragraph(new Run(new Text("No link")));

		var runs = RunElementParser.ParseParagraphRuns(paragraph);

		runs.Should().ContainSingle();
		runs[0].HyperlinkUri.Should().BeNull();
	}

	[Fact]
	public void ResolveHyperlinkUri_ExternalRelationship_ReturnsUrl()
	{
		using var stream = TestDocxBuilder.CreateDocxWithExternalHyperlink("https://example.com/path?q=1");
		using var doc = DocxDocument.Load(stream);

		var hyperlink = doc.DocumentBody.Descendants<Hyperlink>().First();
		var uri = RunElementParser.ResolveHyperlinkUri(hyperlink, doc.MainDocumentPart);

		uri.Should().Be("https://example.com/path?q=1");
	}

	[Fact]
	public void ResolveHyperlinkUri_AnchorOnly_ReturnsPrefixed()
	{
		var hyperlink = new Hyperlink { Anchor = "MySection" };

		var uri = RunElementParser.ResolveHyperlinkUri(hyperlink, null);

		uri.Should().Be("#MySection");
	}

	[Fact]
	public void ResolveHyperlinkUri_NeitherIdNorAnchor_ReturnsNull()
	{
		var hyperlink = new Hyperlink();

		var uri = RunElementParser.ResolveHyperlinkUri(hyperlink, null);

		uri.Should().BeNull();
	}
}
