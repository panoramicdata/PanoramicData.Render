namespace PanoramicData.Render;

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
			ParagraphProperties = paragraphDefaults is null
				? new ParagraphPropertiesBaseStyle()
				: (ParagraphPropertiesBaseStyle)paragraphDefaults.CloneNode(true),
			RunProperties = runDefaults is null
				? new RunPropertiesBaseStyle()
				: (RunPropertiesBaseStyle)runDefaults.CloneNode(true)
		};
	}
}
