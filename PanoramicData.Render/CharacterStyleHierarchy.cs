namespace PanoramicData.Render;

/// <summary>
/// Represents parsed character styles and resolved inheritance chains.
/// </summary>
internal sealed class CharacterStyleHierarchy
{
	private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _chains;

	/// <summary>
	/// Initializes a new instance of the <see cref="CharacterStyleHierarchy"/> class.
	/// </summary>
	/// <param name="styles">Parsed character styles keyed by style ID.</param>
	/// <param name="chains">Resolved inheritance chains keyed by style ID.</param>
	public CharacterStyleHierarchy(
		IReadOnlyDictionary<string, CharacterStyleInfo> styles,
		IReadOnlyDictionary<string, IReadOnlyList<string>> chains)
	{
		Styles = styles;
		_chains = chains;
	}

	/// <summary>
	/// Gets all parsed character styles keyed by style ID.
	/// </summary>
	public IReadOnlyDictionary<string, CharacterStyleInfo> Styles { get; }

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
