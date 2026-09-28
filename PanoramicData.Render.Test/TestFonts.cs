using SkiaSharp;

namespace PanoramicData.Render.Test;

/// <summary>
/// The font the tests measure, shape and lay out text with. It ships with the tests (see
/// TestData/Fonts/README.md) so that results do not depend on which fonts, if any, the machine running
/// them has installed: the CI runner image has none at all.
/// </summary>
internal static class TestFonts
{
	/// <summary>The family name the shipped font reports.</summary>
	public const string SansFamily = "Liberation Sans";

	/// <summary>Directory holding the shipped font, copied to the test output directory.</summary>
	public static string Directory { get; } = Path.Combine(AppContext.BaseDirectory, "TestData", "Fonts");

	/// <summary>Full path to Liberation Sans Regular.</summary>
	public static string SansRegularPath { get; } = Path.Combine(Directory, "LiberationSans-Regular.ttf");

	/// <summary>
	/// A process-wide instance of the shipped typeface, for tests that do not dispose what they are given.
	/// Never dispose it; use <see cref="CreateSans"/> for a typeface the caller owns.
	/// </summary>
	public static SKTypeface Sans { get; } = CreateSans();

	/// <summary>Loads a new instance of the shipped typeface, owned (and disposable) by the caller.</summary>
	public static SKTypeface CreateSans()
		=> SKTypeface.FromFile(SansRegularPath)
			?? throw new InvalidOperationException($"The test font could not be loaded from '{SansRegularPath}'.");

	/// <summary>
	/// A resolver that indexes only the shipped font and substitutes it for Arial, for code under test
	/// that asks for a typeface by family name.
	/// </summary>
	public static FontResolver CreateResolver() => new(new RenderOptions
	{
		FontDirectories = [Directory],
		FontSubstitutions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["Arial"] = SansFamily
		}
	});
}
