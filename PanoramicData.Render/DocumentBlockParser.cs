namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;
using OoxmlSectionProperties = DocumentFormat.OpenXml.Wordprocessing.SectionProperties;

/// <summary>
/// Parses the top-level elements of a document body into <see cref="DocumentBlock"/> instances.
/// </summary>
internal static class DocumentBlockParser
{
	/// <summary>
	/// Parses all top-level elements in the document body into an ordered list of blocks.
	/// Paragraphs become <see cref="ParagraphBlock"/>, tables become <see cref="TablePlaceholderBlock"/>,
	/// and section breaks (in paragraph properties) become <see cref="SectionBreakBlock"/>.
	/// </summary>
	/// <param name="body">The document body to parse.</param>
	/// <returns>An ordered list of <see cref="DocumentBlock"/> instances.</returns>
	public static IReadOnlyList<DocumentBlock> Parse(Body body)
	{
		ArgumentNullException.ThrowIfNull(body);

		var blocks = new List<DocumentBlock>();

		foreach (var element in body.ChildElements)
		{
			switch (element)
			{
				case Paragraph paragraph:
					blocks.Add(CreateParagraphBlock(paragraph));

					// Check for section break in paragraph properties
					var sectPr = paragraph.ParagraphProperties?.GetFirstChild<OoxmlSectionProperties>();
					if (sectPr is not null)
					{
						blocks.Add(new SectionBreakBlock
						{
							SectionInfo = SectionInfoParser.Parse(sectPr)
						});
					}

					break;

				case Table table:
					blocks.Add(new TablePlaceholderBlock { TableElement = table });
					break;
			}
		}

		return blocks;
	}

	internal static ParagraphBlock CreateParagraphBlock(Paragraph paragraph)
	{
		var pPr = paragraph.ParagraphProperties;
		var numPr = pPr?.NumberingProperties;

		var bookmarkStarts = paragraph.Elements<BookmarkStart>()
			.Where(bs => bs.Id?.Value is not null && bs.Name?.Value is not null)
			.Select(bs => new BookmarkStartInfo(int.Parse(bs.Id!.Value!, System.Globalization.CultureInfo.InvariantCulture), bs.Name!.Value!))
			.ToArray();

		var bookmarkEnds = paragraph.Elements<BookmarkEnd>()
			.Where(be => be.Id?.Value is not null)
			.Select(be => new BookmarkEndInfo(int.Parse(be.Id!.Value!, System.Globalization.CultureInfo.InvariantCulture)))
			.ToArray();

		return new ParagraphBlock
		{
			SourceElement = paragraph,
			StyleId = pPr?.ParagraphStyleId?.Val?.Value,
			NumberingId = numPr?.NumberingId?.Val?.Value,
			NumberingLevel = numPr?.NumberingLevelReference?.Val?.Value,
			PageBreakBefore = pPr?.PageBreakBefore is { } pbb
				&& (pbb.Val is null || pbb.Val.Value),
			BookmarkStarts = bookmarkStarts,
			BookmarkEnds = bookmarkEnds,
			IsBiDi = pPr?.BiDi is { } bidi
				&& (bidi.Val is null || bidi.Val.Value),
			Alignment = MapJustification(pPr?.Justification)
		};
	}

	/// <summary>
	/// Maps an OpenXML <see cref="Justification"/> element to a <see cref="ParagraphAlignment"/> value.
	/// </summary>
	private static ParagraphAlignment? MapJustification(Justification? jc)
	{
		if (jc?.Val?.Value is not { } value)
		{
			return null;
		}

		if (value == JustificationValues.Left || value == JustificationValues.Start)
		{
			return ParagraphAlignment.Left;
		}

		if (value == JustificationValues.Center)
		{
			return ParagraphAlignment.Center;
		}

		if (value == JustificationValues.Right || value == JustificationValues.End)
		{
			return ParagraphAlignment.Right;
		}

		if (value == JustificationValues.Both || value == JustificationValues.Distribute)
		{
			return ParagraphAlignment.Justified;
		}

		return null;
	}
}
