namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Parses document-level default paragraph and run properties from <c>w:docDefaults</c>.
/// </summary>
internal static class DocumentDefaultsParser
{
	/// <summary>
	/// Parses document defaults from the styles part.
	/// </summary>
	/// <param name="stylesPart">The styles part, or <see langword="null"/> if not present.</param>
	/// <returns>A <see cref="DocumentDefaults"/> containing parsed defaults.</returns>
	public static DocumentDefaults Parse(StyleDefinitionsPart? stylesPart)
	{
		var docDefaults = stylesPart?.Styles?.DocDefaults;

		var paragraphDefaults = docDefaults?
			.GetFirstChild<ParagraphPropertiesDefault>()?
			.GetFirstChild<ParagraphPropertiesBaseStyle>();

		var runDefaults = docDefaults?
			.GetFirstChild<RunPropertiesDefault>()?
			.GetFirstChild<RunPropertiesBaseStyle>();

		return new DocumentDefaults
		{
			ParagraphProperties = CloneOrCreate(paragraphDefaults),
			RunProperties = CloneOrCreate(runDefaults)
		};
	}

	private static T CloneOrCreate<T>(T? source)
		where T : OpenXmlElement, new()
		=> source is null ? new T() : (T)source.CloneNode(true);
}