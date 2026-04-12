namespace PanoramicData.Render.Test;

using SkiaSharp;
using System;

/// <summary>
/// Compares two images using the Structural Similarity Index (SSIM) algorithm.
/// Returns a value between 0 (completely different) and 1 (identical).
/// </summary>
internal static class PerceptualImageDiff
{
	private const float C1 = 6.5025f;   // (0.01 * 255)^2
	private const float C2 = 58.5225f;  // (0.03 * 255)^2

	/// <summary>
	/// Computes the SSIM between two images.
	/// </summary>
	/// <param name="expected">The baseline (reference) image.</param>
	/// <param name="actual">The rendered (actual) image.</param>
	/// <returns>A value between 0 and 1, where 1 means identical.</returns>
	public static float ComputeSsim(SKBitmap expected, SKBitmap actual)
	{
		ArgumentNullException.ThrowIfNull(expected);
		ArgumentNullException.ThrowIfNull(actual);

		if (expected.Width != actual.Width || expected.Height != actual.Height)
		{
			throw new ArgumentException(
				$"Image dimensions must match. Expected: {expected.Width}x{expected.Height}, Actual: {actual.Width}x{actual.Height}");
		}

		var width = expected.Width;
		var height = expected.Height;

		if (width == 0 || height == 0)
		{
			return 1f; // Both empty → identical
		}

		// Convert to grayscale luminance for SSIM comparison
		var expectedLum = ToLuminance(expected);
		var actualLum = ToLuminance(actual);

		return ComputeSsimFromLuminance(expectedLum, actualLum, width, height);
	}

	/// <summary>
	/// Computes the SSIM between two PNG byte arrays.
	/// </summary>
	/// <param name="expectedPng">The baseline PNG bytes.</param>
	/// <param name="actualPng">The rendered PNG bytes.</param>
	/// <returns>A value between 0 and 1, where 1 means identical.</returns>
	public static float ComputeSsim(byte[] expectedPng, byte[] actualPng)
	{
		using var expectedBitmap = SKBitmap.Decode(expectedPng);
		using var actualBitmap = SKBitmap.Decode(actualPng);
		return ComputeSsim(expectedBitmap, actualBitmap);
	}

	/// <summary>
	/// Creates a diff image highlighting differences between two images.
	/// Pixels that differ are shown in the diff color; identical pixels are shown in white.
	/// </summary>
	/// <param name="expected">The baseline image.</param>
	/// <param name="actual">The rendered image.</param>
	/// <param name="threshold">Per-pixel luminance difference threshold (0–255). Differences below this are ignored.</param>
	/// <returns>A PNG byte array of the diff image.</returns>
	public static byte[] CreateDiffImage(SKBitmap expected, SKBitmap actual, int threshold = 10)
	{
		ArgumentNullException.ThrowIfNull(expected);
		ArgumentNullException.ThrowIfNull(actual);

		if (expected.Width != actual.Width || expected.Height != actual.Height)
		{
			throw new ArgumentException("Image dimensions must match for diff generation.");
		}

		var width = expected.Width;
		var height = expected.Height;

		using var diffBitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);

		for (var y = 0; y < height; y++)
		{
			for (var x = 0; x < width; x++)
			{
				var ep = expected.GetPixel(x, y);
				var ap = actual.GetPixel(x, y);

				var lumDiff = Math.Abs(Luminance(ep) - Luminance(ap));

				diffBitmap.SetPixel(x, y, lumDiff > threshold
					? new SKColor(255, 0, 0, (byte)Math.Min(255, lumDiff * 2)) // Red with intensity proportional to diff
					: SKColors.White);
			}
		}

		using var image = SKImage.FromBitmap(diffBitmap);
		using var data = image.Encode(SKEncodedImageFormat.Png, 100);
		return data.ToArray();
	}

	private static float[] ToLuminance(SKBitmap bitmap)
	{
		var width = bitmap.Width;
		var height = bitmap.Height;
		var lum = new float[width * height];

		for (var y = 0; y < height; y++)
		{
			for (var x = 0; x < width; x++)
			{
				var pixel = bitmap.GetPixel(x, y);
				lum[y * width + x] = Luminance(pixel);
			}
		}

		return lum;
	}

	private static float Luminance(SKColor color) =>
		0.299f * color.Red + 0.587f * color.Green + 0.114f * color.Blue;

	private static float ComputeSsimFromLuminance(float[] x, float[] y, int width, int height)
	{
		// Use 8x8 sliding window SSIM
		const int windowSize = 8;
		var windowCount = 0;
		var ssimSum = 0.0;

		for (var wy = 0; wy <= height - windowSize; wy += windowSize)
		{
			for (var wx = 0; wx <= width - windowSize; wx += windowSize)
			{
				var ssim = ComputeWindowSsim(x, y, width, wx, wy, windowSize);
				ssimSum += ssim;
				windowCount++;
			}
		}

		return windowCount == 0 ? 1f : (float)(ssimSum / windowCount);
	}

	private static double ComputeWindowSsim(float[] x, float[] y, int stride, int startX, int startY, int size)
	{
		var n = size * size;
		double sumX = 0, sumY = 0, sumXx = 0, sumYy = 0, sumXy = 0;

		for (var dy = 0; dy < size; dy++)
		{
			for (var dx = 0; dx < size; dx++)
			{
				var idx = (startY + dy) * stride + (startX + dx);
				double px = x[idx];
				double py = y[idx];
				sumX += px;
				sumY += py;
				sumXx += px * px;
				sumYy += py * py;
				sumXy += px * py;
			}
		}

		var meanX = sumX / n;
		var meanY = sumY / n;
		var varX = sumXx / n - meanX * meanX;
		var varY = sumYy / n - meanY * meanY;
		var covXy = sumXy / n - meanX * meanY;

		var numerator = (2 * meanX * meanY + C1) * (2 * covXy + C2);
		var denominator = (meanX * meanX + meanY * meanY + C1) * (varX + varY + C2);

		return numerator / denominator;
	}
}
