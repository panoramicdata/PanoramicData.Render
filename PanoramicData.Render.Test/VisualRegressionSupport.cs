namespace PanoramicData.Render.Test;

using SkiaSharp;

/// <summary>
/// Shared helpers for the visual regression tests that compare rendered SVG against Word reference PNGs.
/// </summary>
internal static class VisualRegressionSupport
{
	public static string GetAssetsDirectory()
	{
		var current = new DirectoryInfo(AppContext.BaseDirectory);
		while (current is not null)
		{
			if (File.Exists(Path.Combine(current.FullName, "PanoramicData.Render.slnx")))
			{
				return Path.Combine(current.FullName, "PanoramicData.Render.Test", "test-assets");
			}

			current = current.Parent;
		}

		throw new DirectoryNotFoundException("Could not locate repository root from test base directory.");
	}

	public static int CountReferencePages(string referenceDir, string stem)
		=> Directory.GetFiles(referenceDir, $"{stem}_page-*.png", SearchOption.TopDirectoryOnly).Length;

	public static string ReferencePagePath(string referenceDir, string stem, int pageNumber)
		=> Path.Combine(referenceDir, $"{stem}_page-{pageNumber}.png");

	/// <summary>
	/// Rasterises the SVG at 150 DPI, resizes it to the reference page and returns the structural similarity.
	/// </summary>
	public static float ComputeSsim(string referencePath, string svg)
	{
		var actualPng = SvgRasterizer.RasterizeToPng(svg, 150f);
		var expectedPng = File.ReadAllBytes(referencePath);

		using var expectedBitmap = SKBitmap.Decode(expectedPng);
		using var actualBitmap = SKBitmap.Decode(actualPng);
		using var normalizedActual = NormalizeToSize(actualBitmap, expectedBitmap.Width, expectedBitmap.Height);

		return PerceptualImageDiff.ComputeSsim(expectedBitmap, normalizedActual);
	}

	private static SKBitmap NormalizeToSize(SKBitmap bitmap, int width, int height)
	{
		if (bitmap.Width == width && bitmap.Height == height)
		{
			return bitmap.Copy();
		}

		var resized = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
		using var canvas = new SKCanvas(resized);
		canvas.Clear(SKColors.White);
		canvas.DrawBitmap(bitmap, new SKRect(0, 0, width, height), SKSamplingOptions.Default);
		return resized;
	}
}