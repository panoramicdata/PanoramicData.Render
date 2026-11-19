namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed partial class RunElementParserTests
{
	[Fact]
	public void Parse_NoBreakHyphen_ReturnsNonBreakingHyphenElement()
	{
		Single<NonBreakingHyphenRunElement>(new NoBreakHyphen()).Should().NotBeNull();
	}

	[Fact]
	public void Parse_NoBreakHyphenBetweenText_PreservesOrder()
	{
		var run = new Run(
			new Text("well"),
			new NoBreakHyphen(),
			new Text("known"));

		var elements = RunElementParser.Parse(run);

		elements.Should().HaveCount(3);
		elements[0].Should().BeOfType<TextRunElement>()
			.Which.Text.Should().Be("well");
		elements[1].Should().BeOfType<NonBreakingHyphenRunElement>();
		elements[2].Should().BeOfType<TextRunElement>()
			.Which.Text.Should().Be("known");
	}

	private static OpenXmlElement NoteReference(bool endnote, int? id)
	{
		if (endnote)
		{
			var endnoteReference = new EndnoteReference();
			if (id is { } endnoteId)
			{
				endnoteReference.Id = endnoteId;
			}

			return endnoteReference;
		}

		var footnoteReference = new FootnoteReference();
		if (id is { } footnoteId)
		{
			footnoteReference.Id = footnoteId;
		}

		return footnoteReference;
	}

	private static int ParsedNoteId(bool endnote, RunElement element)
		=> endnote
			? element.Should().BeOfType<EndnoteReferenceRunElement>().Subject.EndnoteId
			: element.Should().BeOfType<FootnoteReferenceRunElement>().Subject.FootnoteId;

	[Theory]
	[InlineData(false, 1, 1)]
	[InlineData(false, 42, 42)]
	[InlineData(false, null, 0)]
	[InlineData(true, 1, 1)]
	[InlineData(true, 99, 99)]
	[InlineData(true, null, 0)]
	public void Parse_NoteReference_ReturnsReferenceElementWithId(bool endnote, int? id, int expectedId)
	{
		var run = new Run(NoteReference(endnote, id));

		ParsedNoteId(endnote, RunElementParser.Parse(run).Should().ContainSingle().Subject).Should().Be(expectedId);
	}

	[Theory]
	[InlineData(false, 3)]
	[InlineData(true, 5)]
	public void Parse_TextWithNoteReference_BothParsed(bool endnote, int id)
	{
		var run = new Run(
			new Text("See"),
			NoteReference(endnote, id));

		var elements = RunElementParser.Parse(run);

		elements.Should().HaveCount(2);
		elements[0].Should().BeOfType<TextRunElement>()
			.Which.Text.Should().Be("See");
		ParsedNoteId(endnote, elements[1]).Should().Be(id);
	}
}
