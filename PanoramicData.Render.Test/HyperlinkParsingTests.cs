namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
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
		var runs = ParseDocxParagraph(TestDocxBuilder.CreateDocxWithExternalHyperlink("https://example.com"), 0);

		runs.Should().ContainSingle();
		AssertLink(runs[0], "https://example.com", "Click here");
		runs[0].Elements.Should().ContainSingle();
	}

	[Fact]
	public void ParseParagraphRuns_InternalBookmarkHyperlink_ResolvedAsAnchor()
	{
		var runs = ParseDocxParagraph(TestDocxBuilder.CreateDocxWithInternalBookmarkHyperlink("Chapter1"), 1);

		runs.Should().ContainSingle();
		AssertLink(runs[0], "#Chapter1", "Go to bookmark");
		runs[0].Elements.Should().ContainSingle();
	}

	[Fact]
	public void ParseParagraphRuns_MixedHyperlinksAndPlainRuns_PreservesOrder()
	{
		var runs = ParseDocxParagraph(TestDocxBuilder.CreateDocxWithMixedHyperlinks(), 0);

		runs.Should().HaveCount(5);
		AssertLink(runs[0], null, "Before ");
		AssertLink(runs[1], "https://example.com", "external link");
		runs[2].HyperlinkUri.Should().BeNull();
		runs[3].HyperlinkUri.Should().Be("#Section1");
		runs[4].HyperlinkUri.Should().BeNull();
	}

	[Fact]
	public void ParseParagraphRuns_HyperlinkWithoutPart_ReturnsNullUri()
	{
		var runs = Parse(new Hyperlink(new Run(new Text("Link"))) { Id = "rId99" });

		runs.Should().ContainSingle();
		AssertLink(runs[0], null, "Link");
	}

	[Fact]
	public void ParseParagraphRuns_AnchorLinkWithoutPart_StillResolvesAnchor()
	{
		var runs = Parse(new Hyperlink(new Run(new Text("Link"))) { Anchor = "Section2" });

		runs.Should().ContainSingle();
		runs[0].HyperlinkUri.Should().Be("#Section2");
	}

	[Fact]
	public void ParseParagraphRuns_HyperlinkWithNoIdOrAnchor_ReturnsNullUri()
	{
		var runs = Parse(new Hyperlink(new Run(new Text("Broken link"))));

		runs.Should().ContainSingle();
		runs[0].HyperlinkUri.Should().BeNull();
	}

	[Fact]
	public void ParseParagraphRuns_HyperlinkWithMultipleInnerRuns_AllCarryUri()
	{
		using var stream = TestDocxBuilder.CreateDocxWithExternalHyperlink("https://example.org");
		using var doc = DocxDocument.Load(stream);

		var runs = Parse(new Hyperlink(
			new Run(new Text("First ")),
			new Run(new Text("Second")))
		{ Anchor = "MultiRun" });

		runs.Should().HaveCount(2);
		runs[0].HyperlinkUri.Should().Be("#MultiRun");
		runs[1].HyperlinkUri.Should().Be("#MultiRun");
	}

	[Fact]
	public void ParseParagraphRuns_PlainRunHasNullHyperlinkUri()
	{
		var runs = Parse(new Run(new Text("No link")));

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
		=> RunElementParser.ResolveHyperlinkUri(new Hyperlink { Anchor = "MySection" }, null)
			.Should().Be("#MySection");

	[Fact]
	public void ResolveHyperlinkUri_NeitherIdNorAnchor_ReturnsNull()
		=> RunElementParser.ResolveHyperlinkUri(new Hyperlink(), null).Should().BeNull();

	private static IReadOnlyList<ParsedRun> Parse(params OpenXmlElement[] children)
		=> RunElementParser.ParseParagraphRuns(new Paragraph(children));

	private static IReadOnlyList<ParsedRun> ParseDocxParagraph(Stream stream, int paragraphIndex)
	{
		using (stream)
		{
			using var doc = DocxDocument.Load(stream);
			var paragraph = doc.DocumentBody.Elements<Paragraph>().ElementAt(paragraphIndex);
			return RunElementParser.ParseParagraphRuns(paragraph, doc.MainDocumentPart);
		}
	}

	private static void AssertLink(ParsedRun run, string? uri, string text)
	{
		run.HyperlinkUri.Should().Be(uri);
		run.Elements[0].Should().BeOfType<TextRunElement>()
			.Which.Text.Should().Be(text);
	}
}
