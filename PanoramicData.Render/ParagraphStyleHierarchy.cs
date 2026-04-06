namespace PanoramicData.Render;

/// <summary>
/// Represents parsed paragraph styles and resolved inheritance chains.
/// </summary>
internal sealed class ParagraphStyleHierarchy
{
	private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _chains;

	/// <summary>
	/// Initializes a new instance of the <see cref="ParagraphStyleHierarchy"/> class.
	/// </summary>
	/// <param name="styles">Parsed paragraph styles keyed by style ID.</param>
	/// <param name="chains">Resolved inheritance chains keyed by style ID.</param>
	public ParagraphStyleHierarchy(
		IReadOnlyDictionary<string, ParagraphStyleInfo> styles,
		IReadOnlyDictionary<string, IReadOnlyList<string>> chains)
	{
		Styles = styles;
		_chains = chains;
	}

	/// <summary>
	/// Gets all parsed paragraph styles keyed by style ID.
	/// </summary>
	public IReadOnlyDictionary<string, ParagraphStyleInfo> Styles { get; }

	/// <summary>
	/// Gets the resolved inheritance chain for a style, starting with the style itself.
	/// </summary>
	/// <param name="styleId">The style ID.</param>
	/// <returns>An ordered style ID chain, or an empty list when the style is unknown.</returns>
	public IReadOnlyList<string> GetInheritanceChain(string styleId)
	{
		if (string.IsNullOrWhiteSpace(styleId))
		{
			return [];
		}

		return _chains.TryGetValue(styleId, out var chain) ? chain : [];
	}
}
