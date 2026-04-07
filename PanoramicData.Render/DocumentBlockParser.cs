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

		return new ParagraphBlock
		{
			SourceElement = paragraph,
			StyleId = pPr?.ParagraphStyleId?.Val?.Value,
			NumberingId = numPr?.NumberingId?.Val?.Value,
			NumberingLevel = numPr?.NumberingLevelReference?.Val?.Value,
			PageBreakBefore = pPr?.PageBreakBefore is { } pbb
				&& (pbb.Val is null || pbb.Val.Value)
		};
	}
}
