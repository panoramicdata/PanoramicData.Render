namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;

public sealed class VisualRegressionThresholdsTests
{
	[Fact]
	public void GetMaxDeviation_KnownDocument_ReturnsConfiguredValue()
	{
		var thresholds = new VisualRegressionThresholds(new Dictionary<string, float>
		{
			["basic-text"] = 0.02f,
			["complex-table"] = 0.05f,
		});

		thresholds.GetMaxDeviation("basic-text").Should().Be(0.02f);
		thresholds.GetMaxDeviation("complex-table").Should().Be(0.05f);
	}

	[Fact]
	public void GetMaxDeviation_UnknownDocument_ReturnsFallbackDefault()
	{
		var thresholds = new VisualRegressionThresholds([]);

		thresholds.GetMaxDeviation("unknown").Should().Be(0.03f);
	}

	[Fact]
	public void GetMaxDeviation_UnknownDocumentWithDefaultKey_ReturnsDefaultKeyValue()
	{
		var thresholds = new VisualRegressionThresholds(new Dictionary<string, float>
		{
			["default"] = 0.04f,
		});

		thresholds.GetMaxDeviation("unknown").Should().Be(0.04f);
	}

	[Fact]
	public void LoadFromJson_SimpleNumberValues_ParsesCorrectly()
	{
		const string json = """{"basic-text": 0.01, "default": 0.03}""";

		var thresholds = VisualRegressionThresholds.LoadFromJson(json);

		thresholds.GetMaxDeviation("basic-text").Should().Be(0.01f);
		thresholds.GetMaxDeviation("other").Should().Be(0.03f);
	}

	[Fact]
	public void LoadFromJson_ObjectWithMaxSsimDeviation_ParsesCorrectly()
	{
		const string json = """{"basic-text": {"maxSsimDeviation": 0.02}, "default": {"maxSsimDeviation": 0.05}}""";

		var thresholds = VisualRegressionThresholds.LoadFromJson(json);

		thresholds.GetMaxDeviation("basic-text").Should().Be(0.02f);
		thresholds.GetMaxDeviation("unknown").Should().Be(0.05f);
	}

	[Fact]
	public void LoadFromFile_NonExistentFile_ReturnsEmptyDefaults()
	{
		var thresholds = VisualRegressionThresholds.LoadFromFile("nonexistent.json");

		thresholds.GetMaxDeviation("any").Should().Be(0.03f);
	}

	[Fact]
	public void LoadFromFile_ExistingFile_LoadsThresholds()
	{
		var tempFile = Path.GetTempFileName();
		try
		{
			File.WriteAllText(tempFile, """{"test-doc": 0.01}""");

			var thresholds = VisualRegressionThresholds.LoadFromFile(tempFile);

			thresholds.GetMaxDeviation("test-doc").Should().Be(0.01f);
		}
		finally
		{
			File.Delete(tempFile);
		}
	}
}
