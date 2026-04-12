namespace PanoramicData.Render.Test;

using SkiaSharp;
using Svg.Skia;

/// <summary>
/// Rasterizes SVG strings to PNG byte arrays for visual regression testing.
/// </summary>
internal static class SvgRasterizer
{
	/// <summary>
	/// Rasterizes an SVG string to a PNG byte array at the specified DPI.
	/// </summary>
	/// <param name="svgContent">The SVG markup string.</param>
	/// <param name="dpi">The output resolution in dots per inch (default 150).</param>
	/// <returns>A PNG-encoded byte array.</returns>
	public static byte[] RasterizeToPng(string svgContent, float dpi = 150f)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(svgContent);

		using var svg = new SKSvg();
		svg.FromSvg(svgContent);

		if (svg.Picture is null)
		{
			throw new InvalidOperationException("Failed to parse SVG content into an SKPicture.");
		}

		var bounds = svg.Picture.CullRect;
		var scale = dpi / 96f; // SVG default is 96 DPI
		var width = (int)Math.Ceiling(bounds.Width * scale);
		var height = (int)Math.Ceiling(bounds.Height * scale);

		// If the picture has no drawable content (empty page), fall back to viewBox dimensions
		if (width <= 0 || height <= 0)
		{
			var (vbWidth, vbHeight) = ParseSvgViewBox(svgContent);
			width = (int)Math.Ceiling(vbWidth * scale);
			height = (int)Math.Ceiling(vbHeight * scale);
		}

		if (width <= 0 || height <= 0)
		{
			throw new InvalidOperationException($"Cannot determine SVG dimensions: CullRect={bounds.Width}×{bounds.Height}");
		}

		using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul))
			?? throw new InvalidOperationException($"Failed to create SKSurface ({width}×{height})");
		var canvas = surface.Canvas;
		canvas.Clear(SKColors.White);
		canvas.Scale(scale);
		canvas.DrawPicture(svg.Picture);
		canvas.Flush();

		using var image = surface.Snapshot();
		using var data = image.Encode(SKEncodedImageFormat.Png, 100);
		return data.ToArray();
	}

	/// <summary>
	/// Extracts width and height from an SVG viewBox attribute.
	/// </summary>
	private static (float Width, float Height) ParseSvgViewBox(string svgContent)
	{
		var match = System.Text.RegularExpressions.Regex.Match(
			svgContent,
			@"viewBox\s*=\s*""[\d.]+\s+[\d.]+\s+([\d.]+)\s+([\d.]+)""");
		if (match.Success
			&& float.TryParse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture, out var w)
			&& float.TryParse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture, out var h))
		{
			return (w, h);
		}

		return (0f, 0f);
	}

	/// <summary>
	/// Rasterizes an SVG string to an <see cref="SKBitmap"/> at the specified DPI.
	/// </summary>
	/// <param name="svgContent">The SVG markup string.</param>
	/// <param name="dpi">The output resolution in dots per inch (default 150).</param>
	/// <returns>An <see cref="SKBitmap"/> containing the rasterized image.</returns>
	public static SKBitmap RasterizeToBitmap(string svgContent, float dpi = 150f)
	{
		var pngBytes = RasterizeToPng(svgContent, dpi);
		return SKBitmap.Decode(pngBytes);
	}
}
