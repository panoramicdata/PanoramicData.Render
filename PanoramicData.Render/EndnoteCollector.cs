namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Collects endnote definitions, filtering out system-internal note types
/// (separator, continuation separator, continuation notice) and returning
/// only user-content endnotes suitable for rendering.
/// </summary>
internal static class EndnoteCollector
{
	/// <summary>
	/// Filters a list of endnote definitions to include only user-content endnotes,
	/// excluding system-internal types such as separators.
	/// </summary>
	/// <param name="definitions">The raw endnote definitions from the parser.</param>
	/// <returns>The user-content endnotes in the original order.</returns>
	public static IReadOnlyList<NoteDefinition> CollectUserEndnotes(IReadOnlyList<NoteDefinition> definitions)
	{
		ArgumentNullException.ThrowIfNull(definitions);

		if (definitions.Count == 0)
		{
			return [];
		}

		var result = new List<NoteDefinition>();
		foreach (var definition in definitions)
		{
			if (IsUserContent(definition))
			{
				result.Add(definition);
			}
		}

		return result;
	}

	/// <summary>
	/// Filters a list of endnote definitions to include only those whose IDs
	/// are referenced in the body text, as identified by the supplied set of IDs.
	/// System-internal types are also excluded.
	/// </summary>
	/// <param name="definitions">The raw endnote definitions from the parser.</param>
	/// <param name="referencedIds">The set of endnote IDs actually referenced in the body.</param>
	/// <returns>The referenced user-content endnotes in the original order.</returns>
	public static IReadOnlyList<NoteDefinition> CollectReferencedEndnotes(
		IReadOnlyList<NoteDefinition> definitions,
		IReadOnlySet<int> referencedIds)
	{
		ArgumentNullException.ThrowIfNull(definitions);
		ArgumentNullException.ThrowIfNull(referencedIds);

		if (definitions.Count == 0 || referencedIds.Count == 0)
		{
			return [];
		}

		var result = new List<NoteDefinition>();
		foreach (var definition in definitions)
		{
			if (IsUserContent(definition) && referencedIds.Contains(definition.Id))
			{
				result.Add(definition);
			}
		}

		return result;
	}

	private static bool IsUserContent(NoteDefinition definition) =>
		definition.Type is null || definition.Type == FootnoteEndnoteValues.Normal;
}
