namespace PanoramicData.Render;

using System.Security;
using System.Text;

internal sealed partial class SvgRenderTarget : IRenderTarget
{
	private string ResolveImageHref(ImageData image)
	{
		if (_options.EmbedImages)
		{
			var base64 = Convert.ToBase64String(image.Data);
			return $"data:{image.ContentType};base64,{base64}";
		}

		if (!string.IsNullOrWhiteSpace(image.SourceUri))
		{
			return image.SourceUri;
		}

		_externalImageCounter++;
		return $"images/image-{_externalImageCounter}{GetImageExtension(image.ContentType)}";
	}

	private float ScaleTwips(float valueTwips)
	{
		return valueTwips * _pixelsPerTwip;
	}

	private static string GetImageExtension(string contentType)
	{
		return contentType.ToLowerInvariant() switch
		{
			"image/png" => ".png",
			"image/jpeg" => ".jpg",
			"image/gif" => ".gif",
			"image/webp" => ".webp",
			_ => ".bin"
		};
	}

	private static float EstimateTextWidthTwips(string text, float sizePoints)
	{
		return text.Length * sizePoints * AverageGlyphWidthFactor;
	}
}
