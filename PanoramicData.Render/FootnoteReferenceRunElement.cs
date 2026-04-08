namespace PanoramicData.Render;

/// <summary>
/// Represents a footnote reference marker within a run.
/// When rendered, this appears as a superscript number in the body text.
/// </summary>
internal sealed class FootnoteReferenceRunElement : RunElement
{
	/// <summary>
	/// Gets the footnote ID that this reference points to.
	/// </summary>
	public required int FootnoteId { get; init; }
}
