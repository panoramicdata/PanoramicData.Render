namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed class EndnoteCollectorTests
{
	[Fact]
	public void CollectUserEndnotes_NullInput_ThrowsArgumentNullException()
	{
		var act = () => EndnoteCollector.CollectUserEndnotes(null!);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("definitions");
	}

	[Fact]
	public void CollectUserEndnotes_EmptyInput_ReturnsEmpty()
	{
		var result = EndnoteCollector.CollectUserEndnotes([]);

		result.Should().BeEmpty();
	}

	[Fact]
	public void CollectUserEndnotes_AllSeparators_ReturnsEmpty()
	{
		var definitions = new List<NoteDefinition>
		{
			new(-1, FootnoteEndnoteValues.Separator, []),
			new(0, FootnoteEndnoteValues.ContinuationSeparator, []),
		};

		var result = EndnoteCollector.CollectUserEndnotes(definitions);

		result.Should().BeEmpty();
	}

	[Fact]
	public void CollectUserEndnotes_NullType_IsUserContent()
	{
		var definition = new NoteDefinition(1, null, [new ParagraphBlock { SourceElement = new Paragraph() }]);

		var result = EndnoteCollector.CollectUserEndnotes([definition]);

		result.Should().ContainSingle()
			.Which.Id.Should().Be(1);
	}

	[Fact]
	public void CollectUserEndnotes_NormalType_IsUserContent()
	{
		var definition = new NoteDefinition(1, FootnoteEndnoteValues.Normal, [new ParagraphBlock { SourceElement = new Paragraph() }]);

		var result = EndnoteCollector.CollectUserEndnotes([definition]);

		result.Should().ContainSingle()
			.Which.Id.Should().Be(1);
	}

	[Fact]
	public void CollectUserEndnotes_MixedTypes_FiltersCorrectly()
	{
		var definitions = new List<NoteDefinition>
		{
			new(-1, FootnoteEndnoteValues.Separator, []),
			new(1, null, [new ParagraphBlock { SourceElement = new Paragraph() }]),
			new(0, FootnoteEndnoteValues.ContinuationSeparator, []),
			new(2, FootnoteEndnoteValues.Normal, [new ParagraphBlock { SourceElement = new Paragraph() }]),
		};

		var result = EndnoteCollector.CollectUserEndnotes(definitions);

		result.Should().HaveCount(2);
		result[0].Id.Should().Be(1);
		result[1].Id.Should().Be(2);
	}

	[Fact]
	public void CollectUserEndnotes_PreservesOrder()
	{
		var definitions = new List<NoteDefinition>
		{
			new(3, null, []),
			new(1, null, []),
			new(2, null, []),
		};

		var result = EndnoteCollector.CollectUserEndnotes(definitions);

		result.Should().HaveCount(3);
		result[0].Id.Should().Be(3);
		result[1].Id.Should().Be(1);
		result[2].Id.Should().Be(2);
	}

	[Fact]
	public void CollectReferencedEndnotes_NullDefinitions_ThrowsArgumentNullException()
	{
		var act = () => EndnoteCollector.CollectReferencedEndnotes(null!, new HashSet<int> { 1 });

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("definitions");
	}

	[Fact]
	public void CollectReferencedEndnotes_NullReferencedIds_ThrowsArgumentNullException()
	{
		var act = () => EndnoteCollector.CollectReferencedEndnotes([], null!);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("referencedIds");
	}

	[Fact]
	public void CollectReferencedEndnotes_EmptyDefinitions_ReturnsEmpty()
	{
		var result = EndnoteCollector.CollectReferencedEndnotes([], new HashSet<int> { 1 });

		result.Should().BeEmpty();
	}

	[Fact]
	public void CollectReferencedEndnotes_EmptyReferencedIds_ReturnsEmpty()
	{
		var definitions = new List<NoteDefinition>
		{
			new(1, null, [new ParagraphBlock { SourceElement = new Paragraph() }]),
		};

		var result = EndnoteCollector.CollectReferencedEndnotes(definitions, new HashSet<int>());

		result.Should().BeEmpty();
	}

	[Fact]
	public void CollectReferencedEndnotes_OnlyMatchingIds_Returned()
	{
		var definitions = new List<NoteDefinition>
		{
			new(1, null, [new ParagraphBlock { SourceElement = new Paragraph() }]),
			new(2, null, [new ParagraphBlock { SourceElement = new Paragraph() }]),
			new(3, null, [new ParagraphBlock { SourceElement = new Paragraph() }]),
		};

		var result = EndnoteCollector.CollectReferencedEndnotes(definitions, new HashSet<int> { 1, 3 });

		result.Should().HaveCount(2);
		result[0].Id.Should().Be(1);
		result[1].Id.Should().Be(3);
	}

	[Fact]
	public void CollectReferencedEndnotes_ExcludesSeparators()
	{
		var definitions = new List<NoteDefinition>
		{
			new(-1, FootnoteEndnoteValues.Separator, []),
			new(1, null, [new ParagraphBlock { SourceElement = new Paragraph() }]),
		};

		var result = EndnoteCollector.CollectReferencedEndnotes(definitions, new HashSet<int> { -1, 1 });

		result.Should().ContainSingle()
			.Which.Id.Should().Be(1);
	}

	[Fact]
	public void CollectReferencedEndnotes_PreservesOrder()
	{
		var definitions = new List<NoteDefinition>
		{
			new(3, null, []),
			new(1, null, []),
			new(2, null, []),
		};

		var result = EndnoteCollector.CollectReferencedEndnotes(definitions, new HashSet<int> { 1, 2, 3 });

		result[0].Id.Should().Be(3);
		result[1].Id.Should().Be(1);
		result[2].Id.Should().Be(2);
	}

	[Fact]
	public void EndnotePlacement_DocumentEnd_IsDefault()
	{
		var position = default(EndnotePlacement);

		position.Should().Be(EndnotePlacement.DocumentEnd);
	}

	[Fact]
	public void EndnotePlacement_SectionEnd_HasValue1()
	{
		((int)EndnotePlacement.SectionEnd).Should().Be(1);
	}
}
