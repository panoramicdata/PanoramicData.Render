namespace PanoramicData.Render;

/// <summary>
/// Represents an endnote reference marker within a run.
/// When rendered, this appears as a superscript number or symbol in the body text.
/// </summary>
internal sealed class EndnoteReferenceRunElement : RunElement
{
	/// <summary>
	/// Gets the endnote ID that this reference points to.
	/// </summary>
	public required int EndnoteId { get; init; }
}
