namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed partial class SectionInfoTests
{
	[Fact]
	public void Parse_WithDefaultSectionProperties_ColumnCountDefaultsToOne()
	{
		ParseSection().ColumnCount.Should().Be(1);
	}

	[Fact]
	public void Parse_WithExplicitColumnCount_ExtractsValue()
	{
		ParseSection(new Columns { ColumnCount = 3 }).ColumnCount.Should().Be(3);
	}

	[Fact]
	public void Parse_WithColumnsNoCountAttribute_DefaultsToOne()
	{
		ParseSection(new Columns()).ColumnCount.Should().Be(1);
	}

	[Fact]
	public void Parse_WithEqualWidthColumns_ExtractsSpacingAndSeparator()
	{
		var result = ParseSection(new Columns
		{
			ColumnCount = 3,
			EqualWidth = true,
			Space = "540",
			Separator = true
		});

		result.ColumnCount.Should().Be(3);
		result.ColumnsEqualWidth.Should().BeTrue();
		result.ColumnSpacingTwips.Should().Be(540);
		result.ColumnSeparator.Should().BeTrue();
		result.Columns.Should().BeEmpty();
	}

	[Fact]
	public void Parse_WithExplicitColumnDefinitions_ExtractsWidthsAndSpacing()
	{
		var result = ParseSection(
			new Columns(
				new Column { Width = "2000", Space = "120" },
				new Column { Width = "2400", Space = "240" },
				new Column { Width = "1800", Space = "0" })
			{
				ColumnCount = 3,
				EqualWidth = false,
				Separator = false
			});

		result.ColumnCount.Should().Be(3);
		result.ColumnsEqualWidth.Should().BeFalse();
		result.ColumnSeparator.Should().BeFalse();
		result.Columns.Should().HaveCount(3);
		result.Columns[0].WidthTwips.Should().Be(2000);
		result.Columns[0].SpaceAfterTwips.Should().Be(120);
		result.Columns[1].WidthTwips.Should().Be(2400);
		result.Columns[1].SpaceAfterTwips.Should().Be(240);
		result.Columns[2].WidthTwips.Should().Be(1800);
		result.Columns[2].SpaceAfterTwips.Should().Be(0);
	}

	[Fact]
	public void Parse_WithColumnsNoSpacing_UsesDefaultColumnSpacing()
	{
		ParseSection(new Columns { ColumnCount = 2, EqualWidth = true }).ColumnSpacingTwips.Should().Be(720);
	}

	[Fact]
	public void Parse_WithNoLineNumbering_ReturnsNull()
	{
		ParseSection().LineNumbering.Should().BeNull();
	}

	[Fact]
	public void Parse_WithLineNumbering_ExtractsAllProperties()
	{
		var result = ParseSection(new LineNumberType
		{
			CountBy = 5,
			Start = 10,
			Restart = LineNumberRestartValues.NewSection,
			Distance = "360"
		});

		result.LineNumbering.Should().NotBeNull();
		result.LineNumbering!.Value.CountBy.Should().Be(5);
		result.LineNumbering.Value.Start.Should().Be(10);
		result.LineNumbering.Value.Restart.Should().Be(LineNumberRestart.NewSection);
		result.LineNumbering.Value.DistanceTwips.Should().Be(360);
	}

	[Fact]
	public void Parse_WithLineNumberingDefaults_ReturnsDefaultValues()
	{
		var result = ParseSection(new LineNumberType());

		result.LineNumbering.Should().NotBeNull();
		result.LineNumbering!.Value.CountBy.Should().Be(1);
		result.LineNumbering.Value.Start.Should().Be(1);
		result.LineNumbering.Value.Restart.Should().Be(LineNumberRestart.NewPage);
		result.LineNumbering.Value.DistanceTwips.Should().Be(0);
	}

	[Fact]
	public void Parse_WithLineNumberingContinuousRestart_ExtractsContinuous()
	{
		ParseRestart(LineNumberRestartValues.Continuous).Should().Be(LineNumberRestart.Continuous);
	}

	[Fact]
	public void Parse_WithLineNumberingNewPageRestart_ExtractsNewPage()
	{
		ParseRestart(LineNumberRestartValues.NewPage).Should().Be(LineNumberRestart.NewPage);
	}

	[Fact]
	public void LineNumberingInfo_PrimaryConstructorDefaults()
	{
		var info = new LineNumberingInfo(CountBy: 1, Start: 1);

		info.CountBy.Should().Be(1);
		info.Start.Should().Be(1);
		info.Restart.Should().Be(LineNumberRestart.NewPage);
		info.DistanceTwips.Should().Be(0);
	}

	private static LineNumberRestart ParseRestart(LineNumberRestartValues value)
		=> ParseSection(new LineNumberType { Restart = value }).LineNumbering!.Value.Restart;
}
