namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;

public class RenderOptionsTests
{
	[Fact]
	public void Constructor_InitializesExpectedDefaults()
	{
		var options = new RenderOptions();

		options.FontDirectories.Should().BeEmpty();
		options.FontSubstitutions.Should().BeEmpty();
		options.FallbackFontFamily.Should().BeEmpty();
		options.TargetDpi.Should().Be(96);
		options.EmbedFonts.Should().BeFalse();
		options.EmbedImages.Should().BeTrue();
		options.PageRange.Should().BeNull();
		options.EnableHyphenation.Should().BeFalse();
		options.ShowHiddenText.Should().BeFalse();
	}

	[Fact]
	public void Properties_CanBeAssigned()
	{
		var options = new RenderOptions
		{
			FontDirectories = ["fonts"],
			FontSubstitutions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				["Requested"] = "Replacement"
			},
			FallbackFontFamily = "Fallback",
			TargetDpi = 144,
			EmbedFonts = true,
			EmbedImages = false,
			PageRange = 1..3,
			EnableHyphenation = true,
			ShowHiddenText = true
		};

		options.FontDirectories.Should().Equal("fonts");
		options.FontSubstitutions["Requested"].Should().Be("Replacement");
		options.FallbackFontFamily.Should().Be("Fallback");
		options.TargetDpi.Should().Be(144);
		options.EmbedFonts.Should().BeTrue();
		options.EmbedImages.Should().BeFalse();
		options.PageRange.Should().Be(1..3);
		options.EnableHyphenation.Should().BeTrue();
		options.ShowHiddenText.Should().BeTrue();
	}
}