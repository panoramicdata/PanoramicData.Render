namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Packaging;

/// <summary>
/// Parses footnote and endnote definition parts into typed model objects.
/// </summary>
internal static class FootnoteEndnoteParser
{
	/// <summary>
	/// Parses all footnote definitions from the document.
	/// </summary>
	/// <param name="mainPart">The main document part.</param>
	/// <returns>An ordered list of footnote definitions.</returns>
	public static IReadOnlyList<NoteDefinition> ParseFootnotes(MainDocumentPart mainPart)
	{
		ArgumentNullException.ThrowIfNull(mainPart);

		var footnotes = mainPart.FootnotesPart?.Footnotes;
		if (footnotes is null)
		{
			return [];
		}

		return ParseDefinitions(footnotes.Elements<Footnote>());
	}

	/// <summary>
	/// Parses all endnote definitions from the document.
	/// </summary>
	/// <param name="mainPart">The main document part.</param>
	/// <returns>An ordered list of endnote definitions.</returns>
	public static IReadOnlyList<NoteDefinition> ParseEndnotes(MainDocumentPart mainPart)
	{
		ArgumentNullException.ThrowIfNull(mainPart);

		var endnotes = mainPart.EndnotesPart?.Endnotes;
		if (endnotes is null)
		{
			return [];
		}

		return ParseDefinitions(endnotes.Elements<Endnote>());
	}

	private static IReadOnlyList<NoteDefinition> ParseDefinitions<TNote>(IEnumerable<TNote> notes)
		where TNote : FootnoteEndnoteType
	{
		var result = new List<NoteDefinition>();
		foreach (var note in notes)
		{
			var noteId = note.Id is null ? 0 : checked((int)note.Id.Value);

			result.Add(new NoteDefinition(
				noteId,
				note.Type?.Value,
				ParseBlocks(note)));
		}

		return result;
	}

	private static IReadOnlyList<DocumentBlock> ParseBlocks(FootnoteEndnoteType note)
	{
		var blocks = new List<DocumentBlock>();
		foreach (var element in note.ChildElements)
		{
			switch (element)
			{
				case Paragraph paragraph:
					blocks.Add(DocumentBlockParser.CreateParagraphBlock(paragraph));
					break;

				case Table table:
					blocks.Add(new TablePlaceholderBlock { TableElement = table });
					break;
			}
		}

		return blocks;
	}
}
