namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public class NumberingStyleResolverTests
{
	[Fact]
	public void ResolveLevel_WithNullNumberingPart_ReturnsNull()
	{
		var result = NumberingStyleResolver.ResolveLevel(null, 1, 0);

		result.Should().BeNull();
	}

	[Fact]
	public void ResolveLevel_WithNumberingPartWithoutRoot_ReturnsNull()
	{
		using var stream = TestDocxBuilder.CreateDocxWithNumberingPartWithoutRoot();
		using var doc = DocxDocument.Load(stream);

		var result = NumberingStyleResolver.ResolveLevel(doc.NumberingPart, 1, 0);

		result.Should().BeNull();
	}

	[Fact]
	public void ResolveLevel_WithMissingNumberingInstance_ReturnsNull()
	{
		var numbering = new Numbering(
			new AbstractNum(new Level(
				new NumberingFormat { Val = NumberFormatValues.Decimal },
				new LevelText { Val = "%1." }) { LevelIndex = 0 })
			{ AbstractNumberId = 10 });

		using var stream = TestDocxBuilder.CreateDocxWithNumbering(numbering);
		using var doc = DocxDocument.Load(stream);

		var result = NumberingStyleResolver.ResolveLevel(doc.NumberingPart, 99, 0);

		result.Should().BeNull();
	}

	[Fact]
	public void ResolveLevel_WithMissingAbstractNumbering_ReturnsNull()
	{
		var numbering = new Numbering(
			new NumberingInstance(new AbstractNumId { Val = 500 }) { NumberID = 15 });

		using var stream = TestDocxBuilder.CreateDocxWithNumbering(numbering);
		using var doc = DocxDocument.Load(stream);

		var result = NumberingStyleResolver.ResolveLevel(doc.NumberingPart, 15, 0);

		result.Should().BeNull();
	}

	[Fact]
	public void ResolveLevel_WithNumberingInstanceMissingAbstractNumId_ReturnsNull()
	{
		var numbering = new Numbering(
			new NumberingInstance { NumberID = 16 });

		using var stream = TestDocxBuilder.CreateDocxWithNumbering(numbering);
		using var doc = DocxDocument.Load(stream);

		var result = NumberingStyleResolver.ResolveLevel(doc.NumberingPart, 16, 0);

		result.Should().BeNull();
	}

	[Fact]
	public void ResolveLevel_ResolvesFromAbstractNumberingLevel()
	{
		var numbering = new Numbering(
			new AbstractNum(
				new Level(
					new StartNumberingValue { Val = 3 },
					new NumberingFormat { Val = NumberFormatValues.UpperRoman },
					new LevelText { Val = "(%1)" })
				{ LevelIndex = 0 })
			{ AbstractNumberId = 100 },
			new NumberingInstance(new AbstractNumId { Val = 100 }) { NumberID = 7 });

		using var stream = TestDocxBuilder.CreateDocxWithNumbering(numbering);
		using var doc = DocxDocument.Load(stream);

		var result = NumberingStyleResolver.ResolveLevel(doc.NumberingPart, 7, 0);

		result.Should().NotBeNull();
		result!.LevelIndex.Should().Be(0);
		result.Start.Should().Be(3);
		result.NumberFormat.Should().Be("upperRoman");
		result.LevelText.Should().Be("(%1)");
	}

	[Fact]
	public void ResolveLevel_WithLevelOverrideLevel_UsesOverrideDefinition()
	{
		var numberInstance = new NumberingInstance(new AbstractNumId { Val = 200 })
		{
			NumberID = 8
		};
		numberInstance.Append(
			new LevelOverride(
				new Level(
					new StartNumberingValue { Val = 5 },
					new NumberingFormat { Val = NumberFormatValues.LowerLetter },
					new LevelText { Val = "%1)" })
				{ LevelIndex = 0 })
			{ LevelIndex = 0 });

		var numbering = new Numbering(
			new AbstractNum(
				new Level(
					new StartNumberingValue { Val = 1 },
					new NumberingFormat { Val = NumberFormatValues.Decimal },
					new LevelText { Val = "%1." })
				{ LevelIndex = 0 })
			{ AbstractNumberId = 200 },
			numberInstance);

		using var stream = TestDocxBuilder.CreateDocxWithNumbering(numbering);
		using var doc = DocxDocument.Load(stream);

		var result = NumberingStyleResolver.ResolveLevel(doc.NumberingPart, 8, 0);

		result.Should().NotBeNull();
		result!.Start.Should().Be(5);
		result.NumberFormat.Should().Be("lowerLetter");
		result.LevelText.Should().Be("%1)");
	}

	[Fact]
	public void ResolveLevel_WithStartOverrideOnly_OverridesStartAndKeepsBaseLevel()
	{
		var numberInstance = new NumberingInstance(new AbstractNumId { Val = 300 })
		{
			NumberID = 9
		};
		numberInstance.Append(new LevelOverride(new StartOverrideNumberingValue { Val = 11 }) { LevelIndex = 2 });

		var numbering = new Numbering(
			new AbstractNum(
				new Level(
					new StartNumberingValue { Val = 1 },
					new NumberingFormat { Val = NumberFormatValues.Decimal },
					new LevelText { Val = "%1." })
				{ LevelIndex = 2 })
			{ AbstractNumberId = 300 },
			numberInstance);

		using var stream = TestDocxBuilder.CreateDocxWithNumbering(numbering);
		using var doc = DocxDocument.Load(stream);

		var result = NumberingStyleResolver.ResolveLevel(doc.NumberingPart, 9, 2);

		result.Should().NotBeNull();
		result!.Start.Should().Be(11);
		result.NumberFormat.Should().Be("decimal");
		result.LevelText.Should().Be("%1.");
	}

	[Fact]
	public void ResolveLevel_WithoutStartValue_DefaultsStartToOne()
	{
		var numbering = new Numbering(
			new AbstractNum(
				new Level(
					new NumberingFormat { Val = NumberFormatValues.Bullet },
					new LevelText { Val = "•" })
				{ LevelIndex = 0 })
			{ AbstractNumberId = 400 },
			new NumberingInstance(new AbstractNumId { Val = 400 }) { NumberID = 10 });

		using var stream = TestDocxBuilder.CreateDocxWithNumbering(numbering);
		using var doc = DocxDocument.Load(stream);

		var result = NumberingStyleResolver.ResolveLevel(doc.NumberingPart, 10, 0);

		result.Should().NotBeNull();
		result!.Start.Should().Be(1);
	}

	[Fact]
	public void ResolveLevel_WithNoBaseOrOverrideLevel_ReturnsNull()
	{
		var numbering = new Numbering(
			new AbstractNum(
				new Level(
					new StartNumberingValue { Val = 1 },
					new NumberingFormat { Val = NumberFormatValues.Decimal },
					new LevelText { Val = "%1." })
				{ LevelIndex = 0 })
			{ AbstractNumberId = 410 },
			new NumberingInstance(new AbstractNumId { Val = 410 }) { NumberID = 11 });

		using var stream = TestDocxBuilder.CreateDocxWithNumbering(numbering);
		using var doc = DocxDocument.Load(stream);

		var result = NumberingStyleResolver.ResolveLevel(doc.NumberingPart, 11, 3);

		result.Should().BeNull();
	}

	[Theory]
	[InlineData(-1)]
	[InlineData(-2)]
	public void ResolveLevel_WithNegativeLevel_ThrowsArgumentOutOfRangeException(int level)
	{
		var act = () => NumberingStyleResolver.ResolveLevel(null, 1, level);

		act.Should().Throw<ArgumentOutOfRangeException>();
	}
}
