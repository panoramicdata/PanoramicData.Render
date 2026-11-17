using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace PanoramicData.Render;

public sealed partial class DocxRenderer
{
	private static Dictionary<string, ImageData> PreLoadImages(DocxDocument doc)
	{
		var store = new MediaStore(doc);
		var ids = store.GetImagePartRelationshipIds();
		var images = new Dictionary<string, ImageData>(ids.Count + 16, StringComparer.Ordinal);
		foreach (var id in ids)
		{
			if (store.TryGetImage(id, out var imageData) && imageData is not null)
			{
				images[id] = imageData;
			}
		}

		// Also load images from header and footer parts.
		// Each header/footer part has its own relationship namespace, so image rIds in those parts
		// are independent of the main document's rIds. We add them only when the key is not already
		// present (body images take precedence in the unlikely case of a key collision).
		var rasterizer = new VectorImageRasterizer();
		foreach (var headerPart in doc.MainDocumentPart.HeaderParts)
		{
			LoadPartImages(headerPart, images, rasterizer);
		}

		foreach (var footerPart in doc.MainDocumentPart.FooterParts)
		{
			LoadPartImages(footerPart, images, rasterizer);
		}

		return images;
	}

	private static void LoadPartImages(OpenXmlPart part, Dictionary<string, ImageData> images, VectorImageRasterizer rasterizer)
	{
		foreach (var partRef in part.Parts)
		{
			if (partRef.OpenXmlPart is not ImagePart imagePart)
			{
				continue;
			}

			var relId = partRef.RelationshipId;
			if (images.ContainsKey(relId))
			{
				// Body image with the same relationship ID takes precedence.
				continue;
			}

			using var stream = imagePart.GetStream(System.IO.FileMode.Open, System.IO.FileAccess.Read);
			using var ms = new System.IO.MemoryStream();
			stream.CopyTo(ms);
			ImageData imageData = new(ms.ToArray(), imagePart.ContentType);
			imageData = rasterizer.RasterizeToPngIfSupported(imageData);
			images[relId] = imageData;
		}
	}

	private static SectionInfo GetBodySectionInfo(DocxDocument doc)
	{
		var bodySectPr = doc.DocumentBody
		.GetFirstChild<DocumentFormat.OpenXml.Wordprocessing.SectionProperties>();

		return bodySectPr is not null
		? SectionInfoParser.Parse(bodySectPr)
		: new SectionInfo();
	}

	/// <summary>
	/// Loads all numbering definitions from the document into <see cref="RenderOptions.NumberingStyles"/>
	/// so the render emitter can format list labels correctly.
	/// Already-configured styles are not overwritten.
	/// </summary>
	private void LoadNumberingStyles(DocxDocument doc, IReadOnlyList<DocumentBlock> blocks)
	{
		var numberingPart = doc.NumberingPart;
		if (numberingPart is null)
		{
			return;
		}

		// Build normalization map: numId → canonical numId for shared abstractNumId.
		// This ensures heading levels that use different numIds but share the same
		// abstract numbering definition share counter state.
		BuildNumberingIdNormalization(numberingPart);

		// Collect all referenced (numId, ilvl) pairs from the parsed blocks
		var referenced = new HashSet<(int NumId, int Level)>();
		foreach (var block in blocks)
		{
			if (block is ParagraphBlock pb
				&& pb.NumberingId is int numId)
			{
				var ilvl = pb.NumberingLevel ?? 0;
				var canonicalId = _options.NumberingIdNormalization.TryGetValue(numId, out var cid) ? cid : numId;
				referenced.Add((canonicalId, ilvl));
			}
		}

		foreach (var (numId, level) in referenced)
		{
			var key = $"{numId}:{level}";
			if (_options.NumberingStyles.ContainsKey(key))
			{
				continue;
			}

			var style = NumberingStyleResolver.ResolveLevel(numberingPart, numId, level);
			if (style is not null)
			{
				_options.NumberingStyles[key] = style;
			}
		}
	}
}
