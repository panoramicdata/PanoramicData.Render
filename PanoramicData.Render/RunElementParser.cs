namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;

/// <summary>
/// Parses OpenXML run elements into <see cref="RunElement"/> instances.
/// </summary>
internal static partial class RunElementParser
{
	/// <summary>
	/// Parses the child elements of a run into an ordered list of <see cref="RunElement"/> instances.
	/// </summary>
	/// <param name="run">The OpenXML run element.</param>
	/// <returns>An ordered list of run elements.</returns>
	public static IReadOnlyList<RunElement> Parse(Run run)
	{
		ArgumentNullException.ThrowIfNull(run);

		var elements = new List<RunElement>();

		foreach (var child in run.ChildElements)
		{
			if (child is Text text)
			{
				elements.Add(new TextRunElement { Text = text.InnerText });
			}
			else if (child is Break br)
			{
				elements.Add(new BreakRunElement { BreakType = ParseBreakType(br) });
			}
			else if (child is TabChar)
			{
				elements.Add(new TabRunElement());
			}
			else if (child is NoBreakHyphen)
			{
				elements.Add(new NonBreakingHyphenRunElement());
			}
			else if (child is Drawing drawing)
			{
				ParseDrawing(drawing, elements);
			}
			else if (child is EmbeddedObject oleObj)
			{
				ParseOleObject(oleObj, elements);
			}
			else if (child is FootnoteReference fnRef)
			{
				elements.Add(new FootnoteReferenceRunElement
				{
					FootnoteId = fnRef.Id is null ? 0 : checked((int)fnRef.Id.Value)
				});
			}
			else if (child is EndnoteReference enRef)
			{
				elements.Add(new EndnoteReferenceRunElement
				{
					EndnoteId = enRef.Id is null ? 0 : checked((int)enRef.Id.Value)
				});
			}
		}

		return elements;
	}

	/// <summary>
	/// Parses a run into a <see cref="ParsedRun"/> containing the style ID and content elements.
	/// </summary>
	/// <param name="run">The OpenXML run element.</param>
	/// <returns>A <see cref="ParsedRun"/> with style and elements.</returns>
	public static ParsedRun ParseRun(Run run)
	{
		ArgumentNullException.ThrowIfNull(run);

		return new ParsedRun
		{
			StyleId = run.RunProperties?.RunStyle?.Val?.Value,
			Elements = Parse(run)
		};
	}

	/// <summary>
	/// Parses all runs within a paragraph, including runs inside <c>w:hyperlink</c> wrappers.
	/// </summary>
	/// <param name="paragraph">The OpenXML paragraph element.</param>
	/// <param name="part">The document part used to resolve hyperlink relationship IDs, or <see langword="null"/> when unavailable.</param>
	/// <returns>An ordered list of <see cref="ParsedRun"/> instances.</returns>
	public static IReadOnlyList<ParsedRun> ParseParagraphRuns(Paragraph paragraph, OpenXmlPart? part = null)
	{
		ArgumentNullException.ThrowIfNull(paragraph);

		var runs = new List<ParsedRun>();
		foreach (var child in paragraph.ChildElements)
		{
			if (child is Run run)
			{
				runs.Add(ParseRun(run));
			}
			else if (child is Hyperlink hyperlink)
			{
				var uri = ResolveHyperlinkUri(hyperlink, part);
				foreach (var innerRun in hyperlink.Elements<Run>())
				{
					var parsed = ParseRun(innerRun);
					runs.Add(new ParsedRun
					{
						StyleId = parsed.StyleId,
						Elements = parsed.Elements,
						HyperlinkUri = uri
					});
				}
			}
		}

		return runs;
	}

	/// <summary>
	/// Resolves the URI for a <c>w:hyperlink</c> element. External links use
	/// <c>r:id</c> to reference a <see cref="HyperlinkRelationship"/> in the part.
	/// Internal bookmark links use <c>w:anchor</c> and are returned as <c>#anchorName</c>.
	/// </summary>
	internal static string? ResolveHyperlinkUri(Hyperlink hyperlink, OpenXmlPart? part)
	{
		// External hyperlink via relationship ID
		var relId = hyperlink.Id?.Value;
		if (!string.IsNullOrEmpty(relId) && part is not null)
		{
			var rel = part.HyperlinkRelationships.FirstOrDefault(r => r.Id == relId);
			if (rel is not null)
			{
				return rel.Uri.OriginalString;
			}
		}

		// Internal bookmark reference
		var anchor = hyperlink.Anchor?.Value;
		if (!string.IsNullOrEmpty(anchor))
		{
			return "#" + anchor;
		}

		return null;
	}

	private static RunBreakType ParseBreakType(Break br)
	{
		if (br.Type is null)
		{
			return RunBreakType.Line;
		}

		if (br.Type.Value == BreakValues.Page)
		{
			return RunBreakType.Page;
		}

		if (br.Type.Value == BreakValues.Column)
		{
			return RunBreakType.Column;
		}

		return RunBreakType.Line;
	}

	private static void ParseOleObject(EmbeddedObject oleObj, List<RunElement> elements)
	{
		// Extract OLE relationship ID from o:OLEObject child.
		var oleRelId = oleObj.Descendants()
			.FirstOrDefault(e => e.LocalName == "OLEObject")
			?.GetAttributes()
			.FirstOrDefault(a => a.LocalName == "id")
			.Value ?? string.Empty;

		// Extract preview image relationship ID from v:imagedata child (local-name match avoids type ambiguity).
		var imageDataRelId = oleObj.Descendants()
			.FirstOrDefault(e => e.LocalName == "imagedata")
			?.GetAttributes()
			.FirstOrDefault(a => a.LocalName == "id")
			.Value ?? string.Empty;

		// Read original size from w:object attributes dxaOrig/dyaOrig (twips → EMU: × 914400/1440).
		const long DefaultEmu = 1905000L; // ~2 inches fallback
		var dxaOrig = oleObj.GetAttributes().FirstOrDefault(a => a.LocalName == "dxaOrig").Value;
		var dyaOrig = oleObj.GetAttributes().FirstOrDefault(a => a.LocalName == "dyaOrig").Value;
		var widthEmu = TryParseTwipsToEmu(dxaOrig) ?? DefaultEmu;
		var heightEmu = TryParseTwipsToEmu(dyaOrig) ?? DefaultEmu;

		elements.Add(new OleObjectRunElement
		{
			RelationshipId = oleRelId,
			WidthEmu = widthEmu,
			HeightEmu = heightEmu,
			PreviewImageRelationshipId = imageDataRelId
		});
	}

	private static long? TryParseTwipsToEmu(string? twipsStr)
	{
		if (!long.TryParse(twipsStr, out var twips))
		{
			return null;
		}

		// Twips → EMU: twips / 1440 * 914400 = twips × 635
		return twips * 635L;
	}
}
