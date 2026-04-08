namespace PanoramicData.Render;

/// <summary>
/// Resolves which header or footer reference applies to a specific page,
/// based on OOXML rules for first-page, even/odd, and default selection.
/// </summary>
internal static class HeaderFooterResolver
{
	/// <summary>
	/// Resolves the header reference that applies to the given page.
	/// </summary>
	/// <param name="section">The section properties for the page.</param>
	/// <param name="isFirstPageOfSection">Whether this is the first page of the section.</param>
	/// <param name="pageNumber">The 1-based page number.</param>
	/// <param name="evenAndOddHeaders">Whether the document uses different even/odd headers.</param>
	/// <returns>
	/// The applicable <see cref="HeaderFooterReference"/>, or <see langword="null"/> if no header applies.
	/// </returns>
	public static HeaderFooterReference? ResolveHeader(
		SectionInfo section,
		bool isFirstPageOfSection,
		int pageNumber,
		bool evenAndOddHeaders)
	{
		ArgumentNullException.ThrowIfNull(section);

		return Resolve(section.HeaderReferences, section.TitlePage, isFirstPageOfSection, pageNumber, evenAndOddHeaders);
	}

	/// <summary>
	/// Resolves the footer reference that applies to the given page.
	/// </summary>
	/// <param name="section">The section properties for the page.</param>
	/// <param name="isFirstPageOfSection">Whether this is the first page of the section.</param>
	/// <param name="pageNumber">The 1-based page number.</param>
	/// <param name="evenAndOddHeaders">Whether the document uses different even/odd headers.</param>
	/// <returns>
	/// The applicable <see cref="HeaderFooterReference"/>, or <see langword="null"/> if no footer applies.
	/// </returns>
	public static HeaderFooterReference? ResolveFooter(
		SectionInfo section,
		bool isFirstPageOfSection,
		int pageNumber,
		bool evenAndOddHeaders)
	{
		ArgumentNullException.ThrowIfNull(section);

		return Resolve(section.FooterReferences, section.TitlePage, isFirstPageOfSection, pageNumber, evenAndOddHeaders);
	}

	private static HeaderFooterReference? Resolve(
		IReadOnlyList<HeaderFooterReference> references,
		bool titlePage,
		bool isFirstPageOfSection,
		int pageNumber,
		bool evenAndOddHeaders)
	{
		if (references.Count == 0)
		{
			return null;
		}

		// First page of section with titlePage enabled → use First reference.
		if (titlePage && isFirstPageOfSection)
		{
			var first = FindByKind(references, HeaderFooterKind.First);
			if (first is not null)
			{
				return first;
			}
		}

		// Even page with evenAndOddHeaders enabled → use Even reference.
		if (evenAndOddHeaders && pageNumber % 2 == 0)
		{
			var even = FindByKind(references, HeaderFooterKind.Even);
			if (even is not null)
			{
				return even;
			}
		}

		// Default reference (used for odd pages, or when specific references are not defined).
		return FindByKind(references, HeaderFooterKind.Default);
	}

	private static HeaderFooterReference? FindByKind(
		IReadOnlyList<HeaderFooterReference> references,
		HeaderFooterKind kind)
	{
		for (var i = 0; i < references.Count; i++)
		{
			if (references[i].Type == kind)
			{
				return references[i];
			}
		}

		return null;
	}
}
