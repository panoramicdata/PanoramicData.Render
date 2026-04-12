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
		options.NumberingStyles.Should().BeEmpty();
		options.FallbackFontFamily.Should().BeEmpty();
		options.TargetDpi.Should().Be(96);
		options.EmbedFonts.Should().BeFalse();
		options.EmbedImages.Should().BeTrue();
		options.PageRange.Should().BeNull();
		options.EnableHyphenation.Should().BeFalse();
		options.FieldUpdate.Should().BeNull();
		options.SourceFilename.Should().BeNull();
		options.ShowHiddenText.Should().BeFalse();
	}

	[Fact]
	public void FieldUpdateOptions_InitializesExpectedDefaults()
	{
		var options = new FieldUpdateOptions();

		options.UpdatePageFields.Should().BeTrue();
		options.UpdateDocumentProperties.Should().BeTrue();
		options.UpdateTableOfContents.Should().BeTrue();
		options.UpdateTableOfFigures.Should().BeTrue();
		options.MaxIterations.Should().Be(3);
	}

	[Fact]
	public void FieldUpdateOptions_MaxIterationsLessThanOne_ThrowsArgumentOutOfRangeException()
	{
		var act = () => new FieldUpdateOptions { MaxIterations = 0 };

		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	public void Properties_CanBeAssigned()
	{
		var fieldUpdate = new FieldUpdateOptions
		{
			UpdatePageFields = false,
			UpdateDocumentProperties = false,
			UpdateTableOfContents = false,
			UpdateTableOfFigures = false,
			MaxIterations = 5
		};

		var options = new RenderOptions
		{
			FontDirectories = ["fonts"],
			FontSubstitutions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				["Requested"] = "Replacement"
			},
			NumberingStyles = new Dictionary<string, NumberingLevelStyle>
			{
				["1:0"] = new NumberingLevelStyle { LevelIndex = 0, Start = 1, NumberFormat = "decimal", LevelText = "%1." }
			},
			FallbackFontFamily = "Fallback",
			TargetDpi = 144,
			EmbedFonts = true,
			EmbedImages = false,
			PageRange = 1..3,
			EnableHyphenation = true,
			FieldUpdate = fieldUpdate,
			SourceFilename = "example.docx",
			ShowHiddenText = true
		};

		options.FontDirectories.Should().Equal("fonts");
		options.FontSubstitutions["Requested"].Should().Be("Replacement");
		options.NumberingStyles["1:0"].LevelText.Should().Be("%1.");
		options.FallbackFontFamily.Should().Be("Fallback");
		options.TargetDpi.Should().Be(144);
		options.EmbedFonts.Should().BeTrue();
		options.EmbedImages.Should().BeFalse();
		options.PageRange.Should().Be(1..3);
		options.EnableHyphenation.Should().BeTrue();
		options.FieldUpdate.Should().BeSameAs(fieldUpdate);
		options.FieldUpdate!.UpdatePageFields.Should().BeFalse();
		options.FieldUpdate.UpdateDocumentProperties.Should().BeFalse();
		options.FieldUpdate.UpdateTableOfContents.Should().BeFalse();
		options.FieldUpdate.UpdateTableOfFigures.Should().BeFalse();
		options.FieldUpdate.MaxIterations.Should().Be(5);
		options.SourceFilename.Should().Be("example.docx");
		options.ShowHiddenText.Should().BeTrue();
	}
}