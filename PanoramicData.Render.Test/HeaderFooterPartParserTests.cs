namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Packaging;
using Xunit;

public class HeaderFooterPartParserTests
{
	[Fact]
	public void ParseHeaders_WithDefaultHeader_ReturnsSingleContent()
	{
		using var stream = TestDocxBuilder.CreateDocxWithDefaultHeader();
		using var doc = DocxDocument.Load(stream);
		var sections = SectionInfoParser.ParseAll(doc.DocumentBody);

		var headers = HeaderFooterPartParser.ParseHeaders(doc.MainDocumentPart, sections[0].HeaderReferences);

		headers.Should().HaveCount(1);
		headers[0].Kind.Should().Be(HeaderFooterKind.Default);
		headers[0].Blocks.Should().HaveCount(1);
		headers[0].Blocks[0].Should().BeOfType<ParagraphBlock>();
	}

	[Fact]
	public void ParseHeaders_WithEmptyReferences_ReturnsEmptyList()
	{
		using var stream = TestDocxBuilder.CreateMinimalDocx();
		using var doc = DocxDocument.Load(stream);

		var headers = HeaderFooterPartParser.ParseHeaders(doc.MainDocumentPart, []);

		headers.Should().BeEmpty();
	}

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
		using var stream = TestDocxBuilder.CreateDocxWithMultipleHeaders();
		using var doc = DocxDocument.Load(stream);
		var sections = SectionInfoParser.ParseAll(doc.DocumentBody);

		var headers = HeaderFooterPartParser.ParseHeaders(doc.MainDocumentPart, sections[0].HeaderReferences);

		headers.Should().HaveCount(2);
		headers.Should().Contain(h => h.Kind == HeaderFooterKind.Default);
		headers.Should().Contain(h => h.Kind == HeaderFooterKind.First);
	}

	[Fact]
	public void ParseHeaders_HeaderTextIsAccessible()
	{
		using var stream = TestDocxBuilder.CreateDocxWithDefaultHeader("Custom Header");
		using var doc = DocxDocument.Load(stream);
		var sections = SectionInfoParser.ParseAll(doc.DocumentBody);

		var headers = HeaderFooterPartParser.ParseHeaders(doc.MainDocumentPart, sections[0].HeaderReferences);

		var paragraphBlock = headers[0].Blocks[0].Should().BeOfType<ParagraphBlock>().Subject;
		paragraphBlock.SourceElement.InnerText.Should().Be("Custom Header");
	}

	[Fact]
	public void ParseHeaders_HeaderWithTable_ContainsTablePlaceholder()
	{
		using var stream = TestDocxBuilder.CreateDocxWithHeaderContainingTable();
		using var doc = DocxDocument.Load(stream);
		var sections = SectionInfoParser.ParseAll(doc.DocumentBody);

		var headers = HeaderFooterPartParser.ParseHeaders(doc.MainDocumentPart, sections[0].HeaderReferences);

		headers.Should().HaveCount(1);
		headers[0].Blocks.Should().HaveCount(2);
		headers[0].Blocks[0].Should().BeOfType<ParagraphBlock>();
		headers[0].Blocks[1].Should().BeOfType<TablePlaceholderBlock>();
	}

	[Fact]
	public void ParseHeaders_RelationshipIdIsPreserved()
	{
		using var stream = TestDocxBuilder.CreateDocxWithDefaultHeader();
		using var doc = DocxDocument.Load(stream);
		var sections = SectionInfoParser.ParseAll(doc.DocumentBody);

		var expectedRelId = sections[0].HeaderReferences[0].RelationshipId;
		var headers = HeaderFooterPartParser.ParseHeaders(doc.MainDocumentPart, sections[0].HeaderReferences);

		headers[0].RelationshipId.Should().Be(expectedRelId);
	}

	[Fact]
	public void ParseHeaders_WithInvalidRelationshipId_SkipsReference()
	{
		using var stream = TestDocxBuilder.CreateMinimalDocx();
		using var doc = DocxDocument.Load(stream);

		var fakeReferences = new List<HeaderFooterReference>
		{
			new(HeaderFooterKind.Default, "rIdFake123")
		};

		var headers = HeaderFooterPartParser.ParseHeaders(doc.MainDocumentPart, fakeReferences);

		headers.Should().BeEmpty();
	}

	[Fact]
	public void ParseFooters_WithDefaultFooter_ReturnsSingleContent()
	{
		using var stream = TestDocxBuilder.CreateDocxWithDefaultFooter();
		using var doc = DocxDocument.Load(stream);
		var sections = SectionInfoParser.ParseAll(doc.DocumentBody);

		var footers = HeaderFooterPartParser.ParseFooters(doc.MainDocumentPart, sections[0].FooterReferences);

		footers.Should().HaveCount(1);
		footers[0].Kind.Should().Be(HeaderFooterKind.Default);
		footers[0].Blocks.Should().HaveCount(1);
		footers[0].Blocks[0].Should().BeOfType<ParagraphBlock>();
	}

	[Fact]
	public void ParseFooters_WithEmptyReferences_ReturnsEmptyList()
	{
		using var stream = TestDocxBuilder.CreateMinimalDocx();
		using var doc = DocxDocument.Load(stream);

		var footers = HeaderFooterPartParser.ParseFooters(doc.MainDocumentPart, []);

		footers.Should().BeEmpty();
	}

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
		using var stream = TestDocxBuilder.CreateDocxWithFooterContainingMixedContent();
		using var doc = DocxDocument.Load(stream);
		var sections = SectionInfoParser.ParseAll(doc.DocumentBody);

		var footers = HeaderFooterPartParser.ParseFooters(doc.MainDocumentPart, sections[0].FooterReferences);

		footers.Should().HaveCount(1);
		footers[0].Blocks.Should().HaveCount(3);
		footers[0].Blocks[0].Should().BeOfType<ParagraphBlock>();
		footers[0].Blocks[1].Should().BeOfType<TablePlaceholderBlock>();
		footers[0].Blocks[2].Should().BeOfType<ParagraphBlock>();
	}

	[Fact]
	public void ParseFooters_WithInvalidRelationshipId_SkipsReference()
	{
		using var stream = TestDocxBuilder.CreateMinimalDocx();
		using var doc = DocxDocument.Load(stream);

		var fakeReferences = new List<HeaderFooterReference>
		{
			new(HeaderFooterKind.Default, "rIdFake456")
		};

		var footers = HeaderFooterPartParser.ParseFooters(doc.MainDocumentPart, fakeReferences);

		footers.Should().BeEmpty();
	}
}
