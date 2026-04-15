namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Parses header and footer parts from the document package into <see cref="HeaderFooterContent"/> instances.
/// </summary>
internal static class HeaderFooterPartParser
{
	/// <summary>
	/// Parses header parts referenced by the given section header references.
	/// </summary>
	/// <param name="mainPart">The main document part containing the header parts.</param>
	/// <param name="references">The header references from the section properties.</param>
	/// <returns>An ordered list of parsed header contents.</returns>
	public static IReadOnlyList<HeaderFooterContent> ParseHeaders(
		MainDocumentPart mainPart,
		IReadOnlyList<HeaderFooterReference> references)
	{
		ArgumentNullException.ThrowIfNull(mainPart);
		ArgumentNullException.ThrowIfNull(references);

		if (references.Count == 0)
		{
			return [];
		}

		var headerPartsByRelId = mainPart.HeaderParts
			.ToDictionary(hp => mainPart.GetIdOfPart(hp));

		var results = new List<HeaderFooterContent>();
		foreach (var reference in references)
		{
			if (!headerPartsByRelId.TryGetValue(reference.RelationshipId, out var headerPart))
			{
				continue;
			}

			var header = headerPart.Header;
			if (header is null)
			{
				continue;
			}

			results.Add(new HeaderFooterContent(
				reference.Type,
				reference.RelationshipId,
				ParseBlocks(header)));
		}

		return results;
	}

	/// <summary>
	/// Parses footer parts referenced by the given section footer references.
	/// </summary>
	/// <param name="mainPart">The main document part containing the footer parts.</param>
	/// <param name="references">The footer references from the section properties.</param>
	/// <returns>An ordered list of parsed footer contents.</returns>
	public static IReadOnlyList<HeaderFooterContent> ParseFooters(
		MainDocumentPart mainPart,
		IReadOnlyList<HeaderFooterReference> references)
	{
		ArgumentNullException.ThrowIfNull(mainPart);
		ArgumentNullException.ThrowIfNull(references);

		if (references.Count == 0)
		{
			return [];
		}

		var footerPartsByRelId = mainPart.FooterParts
			.ToDictionary(fp => mainPart.GetIdOfPart(fp));

		var results = new List<HeaderFooterContent>();
		foreach (var reference in references)
		{
			if (!footerPartsByRelId.TryGetValue(reference.RelationshipId, out var footerPart))
			{
				continue;
			}

			var footer = footerPart.Footer;
			if (footer is null)
			{
				continue;
			}

			results.Add(new HeaderFooterContent(
				reference.Type,
				reference.RelationshipId,
				ParseBlocks(footer)));
		}

		return results;
	}

	private static IReadOnlyList<DocumentBlock> ParseBlocks(OpenXmlCompositeElement container)
	{
		var blocks = new List<DocumentBlock>();
		ParseElements(container.ChildElements, blocks);

		return blocks;
	}

	private static void ParseElements(IEnumerable<OpenXmlElement> elements, List<DocumentBlock> blocks)
	{
		foreach (var element in elements)
		{
			switch (element)
			{
				case Paragraph paragraph:
					blocks.Add(DocumentBlockParser.CreateParagraphBlock(paragraph));
					break;

				case Table table:
					blocks.Add(new TablePlaceholderBlock { TableElement = table });
					break;

				case OpenXmlCompositeElement composite:
					// Header/footer content is often wrapped in SDT containers and other
					// non-renderable wrappers. Recurse until we reach block-level content.
					ParseElements(composite.ChildElements, blocks);
					break;
			}
		}
	}
}
