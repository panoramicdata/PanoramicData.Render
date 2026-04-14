namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Represents a paragraph block in the document.
/// </summary>
internal sealed class ParagraphBlock : DocumentBlock
{
	/// <summary>
	/// Gets the original OpenXML paragraph element.
	/// </summary>
	public required Paragraph SourceElement { get; init; }

	/// <summary>
	/// Gets the paragraph style ID (e.g. "Heading1"), or <see langword="null"/> if none is set.
	/// </summary>
	public string? StyleId { get; init; }

	/// <summary>
	/// Gets or sets the numbering definition ID, or <see langword="null"/> if the paragraph is not numbered.
	/// This may be resolved from the paragraph style cascade after initial parsing.
	/// </summary>
	public int? NumberingId { get; set; }

	/// <summary>
	/// Gets or sets the numbering level (0-based), or <see langword="null"/> if the paragraph is not numbered.
	/// This may be resolved from the paragraph style cascade after initial parsing.
	/// </summary>
	public int? NumberingLevel { get; set; }

	/// <summary>
	/// Gets a value indicating whether the paragraph has the <c>w:pageBreakBefore</c> property set,
	/// meaning it should start on a new page.
	/// </summary>
	public bool PageBreakBefore { get; init; }

	/// <summary>
	/// Gets the bookmark start markers (<c>w:bookmarkStart</c>) found in this paragraph.
	/// </summary>
	public IReadOnlyList<BookmarkStartInfo> BookmarkStarts { get; init; } = [];

	/// <summary>
	/// Gets the bookmark end markers (<c>w:bookmarkEnd</c>) found in this paragraph.
	/// </summary>
	public IReadOnlyList<BookmarkEndInfo> BookmarkEnds { get; init; } = [];

	/// <summary>
	/// Gets a value indicating whether the paragraph has BiDi (<c>w:bidi</c>) set,
	/// meaning the paragraph base direction is right-to-left.
	/// </summary>
	public bool IsBiDi { get; init; }

	/// <summary>
	/// Gets the explicit paragraph alignment, or <see langword="null"/> if not explicitly set.
	/// When <see langword="null"/>, the effective alignment depends on <see cref="IsBiDi"/>:
	/// LTR paragraphs default to left, RTL paragraphs default to right.
	/// </summary>
	public ParagraphAlignment? Alignment { get; init; }

	/// <summary>
	/// Gets the paragraph indentation settings.
	/// </summary>
	public ParagraphIndentation Indentation { get; init; }
}
