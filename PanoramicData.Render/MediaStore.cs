using DocumentFormat.OpenXml.Packaging;

namespace PanoramicData.Render;

/// <summary>
/// Provides access to embedded images and media within a DOCX document.
/// Caches image data after first retrieval to avoid repeated stream reads.
/// </summary>
internal sealed class MediaStore
{
	private readonly MainDocumentPart _mainPart;
	private readonly VectorImageRasterizer _vectorImageRasterizer;
	private readonly Dictionary<string, ImageData> _cache = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="MediaStore"/> class.
	/// </summary>
	/// <param name="document">The loaded DOCX document.</param>
	public MediaStore(DocxDocument document)
	{
		ArgumentNullException.ThrowIfNull(document);
		_mainPart = document.MainDocumentPart;
		_vectorImageRasterizer = new VectorImageRasterizer();
	}

	/// <summary>
	/// Attempts to retrieve image data for the specified relationship ID.
	/// </summary>
	/// <param name="relationshipId">The OpenXML relationship ID referencing an image part.</param>
	/// <param name="imageData">When this method returns <see langword="true"/>, contains the image data; otherwise <see langword="null"/>.</param>
	/// <returns><see langword="true"/> if the image was found; otherwise <see langword="false"/>.</returns>
	public bool TryGetImage(string relationshipId, out ImageData? imageData)
	{
		if (string.IsNullOrEmpty(relationshipId))
		{
			imageData = null;
			return false;
		}

		if (_cache.TryGetValue(relationshipId, out imageData))
		{
			return true;
		}

		if (!_mainPart.TryGetPartById(relationshipId, out var part) || part is not ImagePart imagePart)
		{
			imageData = null;
			return false;
		}

		using var stream = imagePart.GetStream(FileMode.Open, FileAccess.Read);
		using var ms = new MemoryStream();
		stream.CopyTo(ms);

		imageData = new ImageData(ms.ToArray(), imagePart.ContentType);
		imageData = _vectorImageRasterizer.RasterizeToPngIfSupported(imageData);
		_cache[relationshipId] = imageData;
		return true;
	}

	/// <summary>
	/// Returns the relationship IDs of all image parts in the main document part.
	/// </summary>
	/// <returns>A list of relationship IDs.</returns>
	public IReadOnlyList<string> GetImagePartRelationshipIds()
	{
		var ids = new List<string>();
		foreach (var part in _mainPart.ImageParts)
		{
			ids.Add(_mainPart.GetIdOfPart(part));
		}

		return ids;
	}
}
