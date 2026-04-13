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
			MaterializeParagraphProperties(documentDefaults, paragraphStyles, paragraph);

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

	/// <summary>
	/// Materializes effective paragraph properties (alignment, numbering, indentation, spacing, etc.)
	/// from the style cascade onto the paragraph element so downstream parsers can read them directly.
	/// </summary>
	private static void MaterializeParagraphProperties(
		DocumentDefaults documentDefaults,
		ParagraphStyleHierarchy paragraphStyles,
		Paragraph paragraph)
	{
		var effective = new ParagraphProperties();

		EffectiveFormattingResolver.Merge(effective, documentDefaults.ParagraphProperties);

		var styleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
		foreach (var sid in paragraphStyles.GetInheritanceChain(styleId ?? string.Empty).Reverse())
		{
			if (paragraphStyles.Styles.TryGetValue(sid, out var style))
			{
				EffectiveFormattingResolver.Merge(effective, style.Properties);
			}
		}

		// Direct formatting overrides style properties
		EffectiveFormattingResolver.Merge(effective, paragraph.ParagraphProperties);

		paragraph.ParagraphProperties = (ParagraphProperties)effective.CloneNode(true);
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