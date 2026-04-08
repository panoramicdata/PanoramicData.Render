namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;

/// <summary>
/// Parses OpenXML run elements into <see cref="RunElement"/> instances.
/// </summary>
internal static class RunElementParser
{
	/// <summary>
	/// Parses the child elements of a run into an ordered list of <see cref="RunElement"/> instances.
	/// </summary>
	/// <param name="run">The OpenXML run element.</param>
	/// <returns>An ordered list of run elements.</returns>
	public static IReadOnlyList<RunElement> Parse(Run run)
	{
		ArgumentNullException.ThrowIfNull(run);

		var elements = new List<RunElement>();

		foreach (var child in run.ChildElements)
		{
			switch (child)
			{
				case Text text:
					elements.Add(new TextRunElement { Text = text.InnerText });
					break;

				case Break br:
					elements.Add(new BreakRunElement { BreakType = ParseBreakType(br) });
					break;

				case TabChar:
					elements.Add(new TabRunElement());
					break;

				case NoBreakHyphen:
					elements.Add(new NonBreakingHyphenRunElement());
					break;

				case Drawing drawing:
					ParseDrawing(drawing, elements);
					break;

				case FootnoteReference fnRef:
					elements.Add(new FootnoteReferenceRunElement
					{
						FootnoteId = fnRef.Id is null ? 0 : checked((int)fnRef.Id.Value)
					});
					break;

				case EndnoteReference enRef:
					elements.Add(new EndnoteReferenceRunElement
					{
						EndnoteId = enRef.Id is null ? 0 : checked((int)enRef.Id.Value)
					});
					break;
			}
		}

		return elements;
	}

	/// <summary>
	/// Parses a run into a <see cref="ParsedRun"/> containing the style ID and content elements.
	/// </summary>
	/// <param name="run">The OpenXML run element.</param>
	/// <returns>A <see cref="ParsedRun"/> with style and elements.</returns>
	public static ParsedRun ParseRun(Run run)
	{
		ArgumentNullException.ThrowIfNull(run);

		return new ParsedRun
		{
			StyleId = run.RunProperties?.RunStyle?.Val?.Value,
			Elements = Parse(run)
		};
	}

	/// <summary>
	/// Parses all runs within a paragraph.
	/// </summary>
	/// <param name="paragraph">The OpenXML paragraph element.</param>
	/// <returns>An ordered list of <see cref="ParsedRun"/> instances.</returns>
	public static IReadOnlyList<ParsedRun> ParseParagraphRuns(Paragraph paragraph)
	{
		ArgumentNullException.ThrowIfNull(paragraph);

		var runs = new List<ParsedRun>();
		foreach (var run in paragraph.Elements<Run>())
		{
			runs.Add(ParseRun(run));
		}

		return runs;
	}

	private static RunBreakType ParseBreakType(Break br)
	{
		if (br.Type is null)
		{
			return RunBreakType.Line;
		}

		if (br.Type.Value == BreakValues.Page)
		{
			return RunBreakType.Page;
		}

		if (br.Type.Value == BreakValues.Column)
		{
			return RunBreakType.Column;
		}

		return RunBreakType.Line;
	}

	private static void ParseDrawing(Drawing drawing, List<RunElement> elements)
	{
		var inline = drawing.GetFirstChild<DW.Inline>();
		if (inline is null)
		{
			return;
		}

		var extent = inline.Extent;
		var blip = inline.Descendants<A.Blip>().FirstOrDefault();

		elements.Add(new InlineImageRunElement
		{
			RelationshipId = blip?.Embed?.Value ?? string.Empty,
			WidthEmu = extent?.Cx ?? 0,
			HeightEmu = extent?.Cy ?? 0
		});
	}
}
