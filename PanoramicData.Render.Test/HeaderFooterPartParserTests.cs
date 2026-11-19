namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Packaging;
using Xunit;

public class HeaderFooterPartParserTests
{
	[Fact]
	public void ParseHeaders_WithDefaultHeader_ReturnsSingleContent()
	{
		var headers = ParseHeadersOf(TestDocxBuilder.CreateDocxWithDefaultHeader());

		AssertSingleDefault(headers, typeof(ParagraphBlock));
	}

	[Fact]
	public void ParseHeaders_WithEmptyReferences_ReturnsEmptyList()
		=> ParseWith((main, _) => HeaderFooterPartParser.ParseHeaders(main, [])).Should().BeEmpty();

	[Fact]
	public void ParseHeaders_NullMainPart_ThrowsArgumentNullException()
	{
		var act = () => HeaderFooterPartParser.ParseHeaders(null!, []);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void ParseHeaders_NullReferences_ThrowsArgumentNullException()
	{
		using var stream = TestDocxBuilder.CreateMinimalDocx();
		using var doc = DocxDocument.Load(stream);

		var act = () => HeaderFooterPartParser.ParseHeaders(doc.MainDocumentPart, null!);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void ParseHeaders_WithMultipleHeaders_ReturnsAllHeaders()
	{
		var headers = ParseHeadersOf(TestDocxBuilder.CreateDocxWithMultipleHeaders());

		headers.Should().HaveCount(2);
		headers.Should().Contain(h => h.Kind == HeaderFooterKind.Default);
		headers.Should().Contain(h => h.Kind == HeaderFooterKind.First);
	}

	[Fact]
	public void ParseHeaders_HeaderTextIsAccessible()
	{
		var headers = ParseHeadersOf(TestDocxBuilder.CreateDocxWithDefaultHeader("Custom Header"));

		var paragraphBlock = headers[0].Blocks[0].Should().BeOfType<ParagraphBlock>().Subject;
		paragraphBlock.SourceElement.InnerText.Should().Be("Custom Header");
	}

	[Fact]
	public void ParseHeaders_HeaderWithTable_ContainsTablePlaceholder()
	{
		var headers = ParseHeadersOf(TestDocxBuilder.CreateDocxWithHeaderContainingTable());

		headers.Should().HaveCount(1);
		AssertBlockTypes(headers[0], typeof(ParagraphBlock), typeof(TablePlaceholderBlock));
	}

	[Fact]
	public void ParseHeaders_RelationshipIdIsPreserved()
	{
		using var stream = TestDocxBuilder.CreateDocxWithDefaultHeader();
		using var doc = DocxDocument.Load(stream);
		var references = SectionInfoParser.ParseAll(doc.DocumentBody)[0].HeaderReferences;

		var headers = HeaderFooterPartParser.ParseHeaders(doc.MainDocumentPart, references);

		headers[0].RelationshipId.Should().Be(references[0].RelationshipId);
	}

	[Fact]
	public void ParseHeaders_WithInvalidRelationshipId_SkipsReference()
		=> ParseWith((main, _) => HeaderFooterPartParser.ParseHeaders(main, FakeReference("rIdFake123")))
			.Should().BeEmpty();

	[Fact]
	public void ParseFooters_WithDefaultFooter_ReturnsSingleContent()
	{
		var footers = ParseFootersOf(TestDocxBuilder.CreateDocxWithDefaultFooter());

		AssertSingleDefault(footers, typeof(ParagraphBlock));
	}

	[Fact]
	public void ParseFooters_WithEmptyReferences_ReturnsEmptyList()
		=> ParseWith((main, _) => HeaderFooterPartParser.ParseFooters(main, [])).Should().BeEmpty();

	[Fact]
	public void ParseFooters_NullMainPart_ThrowsArgumentNullException()
	{
		var act = () => HeaderFooterPartParser.ParseFooters(null!, []);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void ParseFooters_NullReferences_ThrowsArgumentNullException()
	{
		using var stream = TestDocxBuilder.CreateMinimalDocx();
		using var doc = DocxDocument.Load(stream);

		var act = () => HeaderFooterPartParser.ParseFooters(doc.MainDocumentPart, null!);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void ParseFooters_FooterWithMixedContent_ParsesAllBlocks()
	{
		var footers = ParseFootersOf(TestDocxBuilder.CreateDocxWithFooterContainingMixedContent());

		footers.Should().HaveCount(1);
		AssertBlockTypes(footers[0], typeof(ParagraphBlock), typeof(TablePlaceholderBlock), typeof(ParagraphBlock));
	}

	[Fact]
	public void ParseFooters_WithInvalidRelationshipId_SkipsReference()
		=> ParseWith((main, _) => HeaderFooterPartParser.ParseFooters(main, FakeReference("rIdFake456")))
			.Should().BeEmpty();

	private static void AssertSingleDefault(IReadOnlyList<HeaderFooterContent> contents, Type blockType)
	{
		contents.Should().HaveCount(1);
		contents[0].Kind.Should().Be(HeaderFooterKind.Default);
		AssertBlockTypes(contents[0], blockType);
	}

	private static void AssertBlockTypes(HeaderFooterContent content, params Type[] blockTypes)
	{
		content.Blocks.Should().HaveCount(blockTypes.Length);
		for (var i = 0; i < blockTypes.Length; i++)
		{
			content.Blocks[i].Should().BeOfType(blockTypes[i]);
		}
	}

	private static List<HeaderFooterReference> FakeReference(string relationshipId)
		=> [new(HeaderFooterKind.Default, relationshipId)];

	private static IReadOnlyList<HeaderFooterContent> ParseHeadersOf(Stream stream)
		=> ParseWith((main, sections) => HeaderFooterPartParser.ParseHeaders(main, sections[0].HeaderReferences), stream);

	private static IReadOnlyList<HeaderFooterContent> ParseFootersOf(Stream stream)
		=> ParseWith((main, sections) => HeaderFooterPartParser.ParseFooters(main, sections[0].FooterReferences), stream);

	private static IReadOnlyList<HeaderFooterContent> ParseWith(
		Func<MainDocumentPart, IReadOnlyList<SectionInfo>, IReadOnlyList<HeaderFooterContent>> parse,
		Stream? source = null)
	{
		using var stream = source ?? TestDocxBuilder.CreateMinimalDocx();
		using var doc = DocxDocument.Load(stream);
		return parse(doc.MainDocumentPart, SectionInfoParser.ParseAll(doc.DocumentBody));
	}
}
