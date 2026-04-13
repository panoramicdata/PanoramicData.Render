namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Materializes effective style-cascade formatting onto paragraph runs so downstream
/// render emitters can read consistent run properties without re-resolving style hierarchies.
/// </summary>
internal static class StyleCascadeMaterializer
{
	public static void Apply(DocxDocument document)
	{
		ArgumentNullException.ThrowIfNull(document);

		var documentDefaults = DocumentDefaultsParser.Parse(document.StylesPart);
		var themeInfo = ThemeInfoParser.Parse(document.ThemePart);
		var paragraphStyles = ParagraphStyleHierarchyParser.Parse(document.StylesPart);
		var characterStyles = CharacterStyleHierarchyParser.Parse(document.StylesPart);

		foreach (var paragraph in EnumerateParagraphs(document))
		{
			var runs = paragraph.Descendants<Run>().ToArray();
			for (var i = 0; i < runs.Length; i++)
			{
				var run = runs[i];
				var effective = EffectiveFormattingResolver.Resolve(
					documentDefaults,
					themeInfo,
					numberingStyle: null,
					tableStyle: null,
					paragraphStyles,
					characterStyles,
					paragraph,
					run);

				run.RunProperties = (RunProperties)effective.RunProperties.CloneNode(true);
			}
		}
	}

	private static IEnumerable<Paragraph> EnumerateParagraphs(DocxDocument document)
	{
		foreach (var paragraph in document.DocumentBody.Descendants<Paragraph>())
		{
			yield return paragraph;
		}

		foreach (var headerPart in document.MainDocumentPart.HeaderParts)
		{
			if (headerPart.Header is null)
			{
				continue;
			}

			foreach (var paragraph in headerPart.Header.Descendants<Paragraph>())
			{
				yield return paragraph;
			}
		}

		foreach (var footerPart in document.MainDocumentPart.FooterParts)
		{
			if (footerPart.Footer is null)
			{
				continue;
			}

			foreach (var paragraph in footerPart.Footer.Descendants<Paragraph>())
			{
				yield return paragraph;
			}
		}
	}
}