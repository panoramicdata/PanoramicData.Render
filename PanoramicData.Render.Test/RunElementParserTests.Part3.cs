namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;

public sealed partial class RunElementParserTests
{
	[Fact]
	public void Parse_TextWithEndnoteReference_BothParsed()
	{
		var run = new Run(
			new Text("See"),
			new EndnoteReference { Id = 5 });

		var elements = RunElementParser.Parse(run);

		elements.Should().HaveCount(2);
		elements[0].Should().BeOfType<TextRunElement>()
			.Which.Text.Should().Be("See");
		elements[1].Should().BeOfType<EndnoteReferenceRunElement>()
			.Which.EndnoteId.Should().Be(5);
	}
}
