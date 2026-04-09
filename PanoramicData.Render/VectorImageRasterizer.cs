using SkiaSharp;

namespace PanoramicData.Render;

/// <summary>
/// Performs best-effort rasterization for vector image formats not natively emitted by output drivers.
/// </summary>
internal sealed class VectorImageRasterizer
{
	/// <summary>
	/// Attempts to rasterize WMF/EMF payloads to PNG using SkiaSharp.
	/// </summary>
	/// <param name="imageData">The source image payload.</param>
	/// <returns>The rasterized PNG image when successful; otherwise the original image payload.</returns>
	public ImageData RasterizeToPngIfSupported(ImageData imageData)
	{
		ArgumentNullException.ThrowIfNull(imageData);

		if (!IsWmfOrEmf(imageData.ContentType))
		{
			return imageData;
		}

		try
		{
			using var bitmap = SKBitmap.Decode(imageData.Data);
			if (bitmap is null)
			{
				return imageData;
			}

			using var skImage = SKImage.FromBitmap(bitmap);
			using var encoded = skImage.Encode(SKEncodedImageFormat.Png, quality: 100);
			if (encoded is null)
			{
				return imageData;
			}

			return new ImageData(encoded.ToArray(), "image/png");
		}
		catch
		{
			// Best-effort behavior: keep original bytes when rasterization fails.
			return imageData;
		}
	}

	private static bool IsWmfOrEmf(string? contentType)
	{
		if (string.IsNullOrWhiteSpace(contentType))
		{
			return false;
		}

		return contentType.Equals("image/x-wmf", StringComparison.OrdinalIgnoreCase)
			|| contentType.Equals("image/wmf", StringComparison.OrdinalIgnoreCase)
			|| contentType.Equals("image/x-emf", StringComparison.OrdinalIgnoreCase)
			|| contentType.Equals("image/emf", StringComparison.OrdinalIgnoreCase);
	}
}
