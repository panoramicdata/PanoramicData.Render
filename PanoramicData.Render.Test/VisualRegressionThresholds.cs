namespace PanoramicData.Render.Test;

using System.Text.Json;

/// <summary>
/// Loads and resolves per-document SSIM thresholds for visual regression testing.
/// </summary>
internal sealed class VisualRegressionThresholds
{
	private const float DefaultMaxDeviation = 0.03f;
	private readonly Dictionary<string, float> _thresholds;

	/// <summary>
	/// Initializes a new instance with the given per-document thresholds.
	/// </summary>
	/// <param name="thresholds">A dictionary mapping document names to maximum SSIM deviation values.</param>
	public VisualRegressionThresholds(Dictionary<string, float> thresholds)
	{
		_thresholds = thresholds ?? [];
	}

	/// <summary>
	/// Gets the maximum allowed SSIM deviation for a given document.
	/// Falls back to the "default" key if defined, otherwise uses 0.03.
	/// </summary>
	/// <param name="documentName">The document name (without extension).</param>
	/// <returns>The maximum allowed SSIM deviation (0 = must be identical, 1 = any deviation allowed).</returns>
	public float GetMaxDeviation(string documentName)
	{
		if (_thresholds.TryGetValue(documentName, out var threshold))
		{
			return threshold;
		}

		if (_thresholds.TryGetValue("default", out var defaultThreshold))
		{
			return defaultThreshold;
		}

		return DefaultMaxDeviation;
	}

	/// <summary>
	/// Loads thresholds from a JSON file.
	/// </summary>
	/// <param name="jsonPath">The path to the thresholds JSON file.</param>
	/// <returns>A <see cref="VisualRegressionThresholds"/> instance.</returns>
	public static VisualRegressionThresholds LoadFromFile(string jsonPath)
	{
		if (!File.Exists(jsonPath))
		{
			return new VisualRegressionThresholds([]);
		}

		var json = File.ReadAllText(jsonPath);
		return LoadFromJson(json);
	}

	/// <summary>
	/// Loads thresholds from a JSON string.
	/// </summary>
	/// <param name="json">The JSON string containing threshold mappings.</param>
	/// <returns>A <see cref="VisualRegressionThresholds"/> instance.</returns>
	public static VisualRegressionThresholds LoadFromJson(string json)
	{
		var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
		var raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, options) ?? [];

		var thresholds = new Dictionary<string, float>();
		foreach (var (key, value) in raw)
		{
			if (value.ValueKind == JsonValueKind.Number)
			{
				thresholds[key] = value.GetSingle();
			}
			else if (value.ValueKind == JsonValueKind.Object &&
					 value.TryGetProperty("maxSsimDeviation", out var dev))
			{
				thresholds[key] = dev.GetSingle();
			}
		}

		return new VisualRegressionThresholds(thresholds);
	}
}
