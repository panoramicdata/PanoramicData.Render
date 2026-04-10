namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Parses DrawingML shape text frame content and metadata.
/// </summary>
internal static class ShapeTextFrameParser
{
	/// <summary>
	/// Parses shape text-frame information from an inline or anchor drawing subtree.
	/// </summary>
	/// <param name="drawingRoot">Drawing subtree to inspect.</param>
	/// <returns>Parsed text frame information.</returns>
	public static ShapeTextFrameInfo Parse(OpenXmlElement drawingRoot)
	{
		ArgumentNullException.ThrowIfNull(drawingRoot);

		var textBoxContent = drawingRoot.Descendants().FirstOrDefault(e => e.LocalName == "txbxContent");
		if (textBoxContent is not null)
		{
			var contentBlocks = ParseWordprocessingBlocks(textBoxContent);
			var paragraphLines = textBoxContent.ChildElements
				.Where(e => e.LocalName == "p")
				.Select(ExtractText)
				.Where(text => text.Length > 0)
				.ToList();

			return new ShapeTextFrameInfo
			{
				HasTextFrame = true,
				Blocks = contentBlocks,
				Text = string.Join("\n", paragraphLines)
			};
		}

		var txBody = drawingRoot.Descendants().FirstOrDefault(e => e.LocalName == "txBody");
		if (txBody is null)
		{
			return ShapeTextFrameInfo.None;
		}

		var bodyPr = txBody.ChildElements.FirstOrDefault(e => e.LocalName == "bodyPr");
		var paragraphs = txBody.ChildElements.Where(e => e.LocalName == "p").ToList();
		var lines = paragraphs.Select(ExtractText).Where(text => text.Length > 0).ToList();
		var paragraphBlocks = CreateParagraphBlocks(lines);

		var autoFitMode = ShapeTextAutoFitMode.None;
		if (bodyPr is not null)
		{
			if (bodyPr.ChildElements.Any(e => e.LocalName == "noAutofit"))
			{
				autoFitMode = ShapeTextAutoFitMode.NoAutoFit;
			}
			else if (bodyPr.ChildElements.Any(e => e.LocalName == "normAutofit"))
			{
				autoFitMode = ShapeTextAutoFitMode.NormalAutoFit;
			}
			else if (bodyPr.ChildElements.Any(e => e.LocalName == "spAutoFit"))
			{
				autoFitMode = ShapeTextAutoFitMode.ShapeAutoFit;
			}
		}

		return new ShapeTextFrameInfo
		{
			HasTextFrame = true,
			Blocks = paragraphBlocks,
			Text = string.Join("\n", lines),
			LeftInsetEmu = ParseLongAttribute(bodyPr, "lIns"),
			TopInsetEmu = ParseLongAttribute(bodyPr, "tIns"),
			RightInsetEmu = ParseLongAttribute(bodyPr, "rIns"),
			BottomInsetEmu = ParseLongAttribute(bodyPr, "bIns"),
			AutoFitMode = autoFitMode
		};
	}

	private static IReadOnlyList<DocumentBlock> ParseWordprocessingBlocks(OpenXmlElement textBoxContent)
	{
		var blocks = new List<DocumentBlock>();
		foreach (var child in textBoxContent.ChildElements)
		{
			switch (child.LocalName)
			{
				case "p":
					var paragraph = new Paragraph
					{
						InnerXml = child.InnerXml
					};
					blocks.Add(DocumentBlockParser.CreateParagraphBlock(paragraph));
					break;
				case "tbl":
					var table = new Table
					{
						InnerXml = child.InnerXml
					};
					blocks.Add(new TablePlaceholderBlock { TableElement = table });
					break;
			}
		}

		return blocks;
	}

	private static IReadOnlyList<DocumentBlock> CreateParagraphBlocks(IReadOnlyList<string> lines)
	{
		var blocks = new List<DocumentBlock>(lines.Count);
		for (var index = 0; index < lines.Count; index++)
		{
			var paragraph = string.IsNullOrEmpty(lines[index])
				? new Paragraph()
				: new Paragraph(new Run(new Text(lines[index])));
			blocks.Add(DocumentBlockParser.CreateParagraphBlock(paragraph));
		}

		return blocks;
	}

	private static string ExtractText(OpenXmlElement paragraphLikeElement)
	{
		return string.Concat(paragraphLikeElement.Descendants().Where(d => d.LocalName == "t").Select(t => t.InnerText));
	}

	private static long ParseLongAttribute(OpenXmlElement? element, string localName)
	{
		if (element is null)
		{
			return 0;
		}

		var attributes = element.GetAttributes();
		for (var i = 0; i < attributes.Count; i++)
		{
			if (attributes[i].LocalName == localName && long.TryParse(attributes[i].Value, out var parsed))
			{
				return parsed;
			}
		}

		return 0;
	}
}
