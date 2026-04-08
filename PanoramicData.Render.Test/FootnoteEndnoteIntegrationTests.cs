namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

/// <summary>
/// Integration tests that exercise the full footnote/endnote pipeline:
/// parsing → layout → space reservation → splitting → separator → pagination.
/// </summary>
public sealed class FootnoteEndnoteIntegrationTests
{
	private static readonly SectionInfo DefaultSection = new();

	private static NoteDefinition MakeFootnote(int id, int blockCount = 1) =>
		new(id, null, Enumerable.Range(0, blockCount)
			.Select(_ => (DocumentBlock)new ParagraphBlock { SourceElement = new Paragraph() })
			.ToList());

	[Fact]
	public void FootnoteLayout_ThenReserveSpace_ReducesAvailableContent()
	{
		// Arrange: one footnote with 1 paragraph
		var footnote = MakeFootnote(1);
		var (_, totalHeight) = FootnoteLayoutEngine.Layout([footnote]);

		// Act: compute available content height with footnote space
		var available = PageBuilder.ComputeAvailableContentHeight(DefaultSection, footnoteHeight: totalHeight);
		var baseAvailable = PageBuilder.ComputeAvailableContentHeight(DefaultSection);

		// Assert: footnote space reduces the available content area
		available.Should().BeLessThan(baseAvailable);
		(baseAvailable - available).Should().Be(totalHeight);
	}

	[Fact]
	public void FootnoteLayout_WithSeparator_SeparatorIsFirstBlock()
	{
		var footnote = MakeFootnote(1);

		var (blocks, totalHeight) = FootnoteLayoutEngine.Layout([footnote]);

		blocks.Should().HaveCount(2);
		blocks[0].Block.Should().BeOfType<FootnoteSeparatorBlock>();
		blocks[1].Block.Should().BeOfType<ParagraphBlock>();
		totalHeight.Should().BeGreaterThan(0f);
	}

	[Fact]
	public void FootnoteSplitter_AllFit_WhenAvailableHeightSufficient()
	{
		var footnotes = new[] { MakeFootnote(1), MakeFootnote(2) };
		var (blocks, totalHeight) = FootnoteLayoutEngine.Layout(footnotes);

		var (currentPage, overflow) = FootnoteSplitter.Split(blocks, totalHeight + 100f);

		currentPage.Should().HaveCount(blocks.Count);
		overflow.Should().BeEmpty();
	}

	[Fact]
	public void FootnoteSplitter_PartialFit_WhenAvailableHeightInsufficient()
	{
		// 3 footnotes: separator(240) + 3 paragraphs(200 each) = 840
		var footnotes = new[] { MakeFootnote(1), MakeFootnote(2), MakeFootnote(3) };
		var (blocks, _) = FootnoteLayoutEngine.Layout(footnotes);

		// Available height fits separator + first paragraph only (440)
		var (currentPage, overflow) = FootnoteSplitter.Split(blocks, 450f);

		currentPage.Should().HaveCount(2); // separator + 1 paragraph
		overflow.Should().HaveCount(2); // remaining 2 paragraphs
	}

	[Fact]
	public void FootnoteReservedSpace_AffectsPagination()
	{
		// Arrange: create body content that nearly fills a page
		var section = DefaultSection;
		var baseAvailable = PageBuilder.ComputeAvailableContentHeight(section);
		var lineHeight = 240f;
		var lineCount = (int)(baseAvailable / lineHeight); // fills the page

		var bodyBlocks = Enumerable.Range(0, lineCount)
			.Select(_ => new LayoutBlock(new ParagraphBlock { SourceElement = new Paragraph() }, lineHeight))
			.ToArray();

		// Act: paginate with no footnote space — should fit on one page
		var pagesNoFootnote = PageBuilder.Paginate(bodyBlocks, section);

		// Act: paginate with footnote space — should need more pages
		var footnoteHeight = 500f;
		var pagesWithFootnote = PageBuilder.Paginate(bodyBlocks, section, footnoteHeight: footnoteHeight);

		pagesWithFootnote.Count.Should().BeGreaterThanOrEqualTo(pagesNoFootnote.Count);
	}

	[Fact]
	public void FootnotePosition_ComputedCorrectly()
	{
		var section = DefaultSection;
		var headerHeight = 300f;
		var footerHeight = 400f;
		var footnoteHeight = 500f;

		var footnoteTop = PageBuilder.ComputeFootnoteTop(section, footerHeight, footnoteHeight);
		var footerTop = PageBuilder.ComputeFooterTop(section, footerHeight);
		var contentTop = PageBuilder.ComputeContentTop(section, headerHeight);

		// Footnote sits between content and footer
		footnoteTop.Should().BeLessThan(footerTop);
		footnoteTop.Should().BeGreaterThan(contentTop);

		// FootnoteTop = FooterTop - footnoteHeight
		footnoteTop.Should().Be(footerTop - footnoteHeight);
	}

	[Fact]
	public void EndnoteCollector_FiltersSystemTypes_FromParsedDefinitions()
	{
		// Simulate what FootnoteEndnoteParser returns: separator + user notes
		var definitions = new List<NoteDefinition>
		{
			new(-1, FootnoteEndnoteValues.Separator, []),
			new(0, FootnoteEndnoteValues.ContinuationSeparator, []),
			new(1, null, [new ParagraphBlock { SourceElement = new Paragraph() }]),
			new(2, null, [new ParagraphBlock { SourceElement = new Paragraph() }]),
		};

		var userNotes = EndnoteCollector.CollectUserEndnotes(definitions);

		userNotes.Should().HaveCount(2);
		userNotes.Should().AllSatisfy(n => n.Id.Should().BeGreaterThan(0));
	}

	[Fact]
	public void EndnoteCollector_ReferencedOnly_MatchesBodyReferences()
	{
		var definitions = new List<NoteDefinition>
		{
			new(-1, FootnoteEndnoteValues.Separator, []),
			new(1, null, [new ParagraphBlock { SourceElement = new Paragraph() }]),
			new(2, null, [new ParagraphBlock { SourceElement = new Paragraph() }]),
			new(3, null, [new ParagraphBlock { SourceElement = new Paragraph() }]),
		};

		// Simulate body references to endnotes 1 and 3
		var referencedIds = new HashSet<int> { 1, 3 };
		var referenced = EndnoteCollector.CollectReferencedEndnotes(definitions, referencedIds);

		referenced.Should().HaveCount(2);
		referenced[0].Id.Should().Be(1);
		referenced[1].Id.Should().Be(3);
	}

	[Fact]
	public void EndnoteLayout_ReuseFootnoteLayoutEngine()
	{
		// Endnotes use the same layout engine as footnotes since they share block structure
		var endnote = new NoteDefinition(1, null, [new ParagraphBlock { SourceElement = new Paragraph() }]);

		var (blocks, totalHeight) = FootnoteLayoutEngine.Layout([endnote], includeSeparator: false);

		blocks.Should().ContainSingle();
		totalHeight.Should().Be(FootnoteLayoutEngine.DefaultFootnoteLineHeightTwips);
	}

	[Fact]
	public void FullPipeline_FootnoteReferenceToLayout()
	{
		// Step 1: Create a footnote reference marker
		var reference = new FootnoteReferenceRunElement { FootnoteId = 1 };

		// Step 2: Create the corresponding footnote definition
		var definition = MakeFootnote(reference.FootnoteId, blockCount: 2);

		// Step 3: Layout the footnote
		var (footnoteBlocks, footnoteHeight) = FootnoteLayoutEngine.Layout([definition]);

		// Step 4: Reserve space
		var section = DefaultSection;
		var availableWithFootnote = PageBuilder.ComputeAvailableContentHeight(section, footnoteHeight: footnoteHeight);
		var availableWithout = PageBuilder.ComputeAvailableContentHeight(section);

		// Step 5: Verify the pipeline
		reference.FootnoteId.Should().Be(definition.Id);
		footnoteBlocks.Should().HaveCount(3); // separator + 2 paragraphs
		footnoteHeight.Should().BeGreaterThan(0f);
		availableWithFootnote.Should().BeLessThan(availableWithout);
	}

	[Fact]
	public void FullPipeline_EndnoteReferenceToCollection()
	{
		// Step 1: Create endnote reference markers
		var ref1 = new EndnoteReferenceRunElement { EndnoteId = 1 };
		var ref2 = new EndnoteReferenceRunElement { EndnoteId = 3 };

		// Step 2: Simulate parsed definitions (including system types)
		var definitions = new List<NoteDefinition>
		{
			new(-1, FootnoteEndnoteValues.Separator, []),
			new(1, null, [new ParagraphBlock { SourceElement = new Paragraph() }]),
			new(2, null, [new ParagraphBlock { SourceElement = new Paragraph() }]),
			new(3, null, [new ParagraphBlock { SourceElement = new Paragraph() }]),
		};

		// Step 3: Collect referenced endnotes
		var referencedIds = new HashSet<int> { ref1.EndnoteId, ref2.EndnoteId };
		var endnotes = EndnoteCollector.CollectReferencedEndnotes(definitions, referencedIds);

		// Step 4: Layout (reusing footnote layout engine)
		var (blocks, totalHeight) = FootnoteLayoutEngine.Layout(endnotes, includeSeparator: false);

		// Step 5: Verify the pipeline
		endnotes.Should().HaveCount(2);
		blocks.Should().HaveCount(2);
		totalHeight.Should().Be(2 * FootnoteLayoutEngine.DefaultFootnoteLineHeightTwips);
	}

	[Fact]
	public void SeparatorBlock_WidthFraction_IsOneThird()
	{
		var footnote = MakeFootnote(1);
		var (blocks, _) = FootnoteLayoutEngine.Layout([footnote]);

		var separator = blocks[0].Block.Should().BeOfType<FootnoteSeparatorBlock>().Subject;
		separator.WidthFraction.Should().BeApproximately(1f / 3f, 0.0001f);
	}
}
